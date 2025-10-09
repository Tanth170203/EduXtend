using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.GGLogin
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        Task<string> GenerateAndSaveRefreshTokenAsync(User user, string deviceInfo);
        Task<User?> ValidateRefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);
    }
}
