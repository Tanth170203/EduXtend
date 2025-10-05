using BusinessObject.DTOs.GGLogin;
using BusinessObject.Enum;
using BusinessObject.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Services.GGLogin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Users
{
    public class GoogleAuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly ITokenService _tokens;
        private readonly GoogleAuthOptions _opt;

        public GoogleAuthService(IUserRepository users, ITokenService tokens, IOptions<GoogleAuthOptions> opt)
        {
            _users = users;
            _tokens = tokens;
            _opt = opt.Value;
        }

        public async Task<TokenResponseDto?> GoogleLoginAsync(string idToken, string deviceInfo)
        {
            // Validate Google ID Token
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = _opt.ClientIds, // must match client id(s)
                HostedDomain = _opt.HostedDomain // restrict to @fpt.edu.vn
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Google token validation failed: {ex.Message}");
                return null;
            }

            var email = payload.Email?.Trim().ToLower();
            var sub = payload.Subject;             // Google unique user id
            var name = payload.Name ?? email ?? sub;

            if (string.IsNullOrEmpty(email) || !email.EndsWith("@" + _opt.HostedDomain, StringComparison.OrdinalIgnoreCase))
                return null;

            // Upsert user (by GoogleSub first, then by Email)
            var user = await _users.FindByGoogleSubAsync(sub) ?? await _users.FindByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    Email = email!,
                    FullName = name,
                    GoogleSubject = sub,
                    IsActive = true,
                };
                await _users.AddAsync(user);
            }
            else
            {
                user.FullName ??= name;
                user.GoogleSubject ??= sub;
                if (!user.IsActive) return null;
                user.LastLoginAt = DateTime.UtcNow;
                await _users.SaveChangesAsync();
            }

            var (access, expires) = _tokens.CreateAccessToken(user);
            var refresh = _tokens.CreateRefreshToken();

            await _users.AddUserTokenAsync(new UserToken
            {
                UserId = user.Id,
                RefreshToken = refresh,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = deviceInfo
            });

            return new TokenResponseDto
            {
                AccessToken = access,
                ExpiresAt = expires,
                RefreshToken = refresh
            };

        }

        public async Task<TokenResponseDto?> RefreshAsync(string refreshToken, string deviceInfo)
        {
            var old = await _users.GetValidRefreshTokenAsync(refreshToken);
            if (old == null || old.ExpiryDate < DateTime.UtcNow || old.User == null || !old.User.IsActive)
                return null;

            old.Revoked = true; old.RevokedAt = DateTime.UtcNow;

            var (access, expires) = _tokens.CreateAccessToken(old.User);
            var newRefresh = _tokens.CreateRefreshToken();

            await _users.AddUserTokenAsync(new UserToken
            {
                UserId = old.UserId,
                RefreshToken = newRefresh,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = deviceInfo
            });

            return new TokenResponseDto
            {
                AccessToken = access,
                ExpiresAt = expires,
                RefreshToken = newRefresh
            };

        }

        public async Task LogoutAsync(string refreshToken)
        {
            var token = await _users.GetValidRefreshTokenAsync(refreshToken);
            if (token != null)
            {
                token.Revoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _users.SaveChangesAsync();
            }
        }
    }
}
