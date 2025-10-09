using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.GGLogin
{
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
        public string? DeviceInfo { get; set; }
    }
}
