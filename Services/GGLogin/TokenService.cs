using BusinessObject.DTOs.GGLogin;
using BusinessObject.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        private readonly JwtOptions _opt;
        public TokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

        public (string token, DateTime expires) CreateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim> {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.FullName ?? user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

            foreach (var ur in user.UserRoles)
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.RoleName));

            var expires = DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes);
            var jwt = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims,
                notBefore: DateTime.UtcNow, expires: expires, signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
        }

        public string CreateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }
    }
}
