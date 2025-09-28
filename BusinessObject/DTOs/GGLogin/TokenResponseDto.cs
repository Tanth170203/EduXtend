using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.GGLogin
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = null!;
    }
}
