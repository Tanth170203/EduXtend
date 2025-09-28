using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.DTOs.GGLogin
{
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = null!;
    }
}
