using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Services.GGLogin
{
    public class GoogleAuthOptions
    {
        public string HostedDomain { get; set; } = "fpt.edu.vn";
        public List<string> ClientIds { get; set; } = new();
    }
}
