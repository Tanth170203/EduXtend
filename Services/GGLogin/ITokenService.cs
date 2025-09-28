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
        (string token, DateTime expires) CreateAccessToken(User user);
        string CreateRefreshToken();
    }
}
