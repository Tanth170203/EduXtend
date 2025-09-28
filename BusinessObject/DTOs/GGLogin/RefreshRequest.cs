using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.GGLogin
{
    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = null!;
    }
}
