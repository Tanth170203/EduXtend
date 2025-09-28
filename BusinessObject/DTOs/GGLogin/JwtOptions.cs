using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.GGLogin
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string Key { get; set; } = null!;
        public int AccessTokenMinutes { get; set; }
        public int RefreshTokenDays { get; set; }
    }
}
