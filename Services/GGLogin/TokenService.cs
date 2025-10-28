using BusinessObject.DTOs.GGLogin;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repositories.Users;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Services.GGLogin
{
    public class TokenService : ITokenService
    {
        private readonly EduXtendContext _context;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<TokenService> _logger;

        public TokenService(EduXtendContext context, IOptions<JwtOptions> jwtOptions, ILogger<TokenService> logger)
        {
            _context = context;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        public string GenerateAccessToken(User user)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);

            var claims = BuildUserClaims(user, now, expires);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            if (string.IsNullOrWhiteSpace(tokenString) || tokenString.Count(c => c == '.') != 2)
            {
                throw new InvalidOperationException("Generated token is malformed");
            }

            return tokenString;
        }

        public async Task<string> GenerateAndSaveRefreshTokenAsync(User user, string deviceInfo)
        {
            var refreshToken = GenerateSecureToken();

            var tokenEntity = new UserToken
            {
                RefreshToken = refreshToken,
                UserId = user.Id,
                DeviceInfo = deviceInfo,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
                CreatedAt = DateTime.UtcNow,
                Revoked = false
            };

            _context.UserTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<User?> ValidateRefreshTokenAsync(string refreshToken)
        {
            var token = await _context.UserTokens
                .Include(t => t.User)
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken
                    && !t.Revoked
                    && t.ExpiryDate > DateTime.UtcNow);

            return token?.User;
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var token = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);

            if (token != null)
            {
                token.Revoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // Helper Methods
        private List<Claim> BuildUserClaims(User user, DateTime issuedAt, DateTime expires)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expires).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                claims.Add(new Claim("avatar", user.AvatarUrl));
            }

            // Add role claim
            if (user.Role != null && !string.IsNullOrEmpty(user.Role.RoleName))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName));
            }

            // ✅ Thêm StudentId vào claims nếu user là Student
            try
            {
                var student = _context.Students.FirstOrDefault(s => s.UserId == user.Id);
                if (student != null)
                {
                    claims.Add(new Claim("StudentId", student.Id.ToString()));
                    claims.Add(new Claim("StudentCode", student.StudentCode));
                    _logger.LogInformation("✅ Added StudentId to JWT: UserId={UserId} → StudentId={StudentId}, StudentCode={StudentCode}", 
                        user.Id, student.Id, student.StudentCode);
                }
                else
                {
                    _logger.LogWarning("⚠️ No Student found for UserId={UserId}. User might not be a Student or Student record missing.", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading Student for UserId={UserId}", user.Id);
            }

            return claims;
        }

        private string GenerateSecureToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
