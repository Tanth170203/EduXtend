using BusinessObject.DTOs.GGLogin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.GGLogin
{
    public interface IAuthService
    {
        Task<TokenResponseDto?> GoogleLoginAsync(string idToken, string deviceInfo);
        Task<TokenResponseDto?> RefreshAsync(string refreshToken, string deviceInfo);
        Task LogoutAsync(string refreshToken);
    }
}
