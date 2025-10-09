using BusinessObject.DTOs.GGLogin;
using BusinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.GGLogin
{
    public interface IGoogleAuthService
    {
        Task<User> LoginWithGoogleAsync(string idToken, string deviceInfo);
    }
}
