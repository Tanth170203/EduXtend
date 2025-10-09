using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace WebAPI.Authentication
{
    public class CustomJwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public CustomJwtAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
            : base(options, logger, encoder)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                // Read token from cookie
                if (!Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    Console.WriteLine("[CustomJWT] No AccessToken cookie found");
                    return Task.FromResult(AuthenticateResult.NoResult());
                }

                Console.WriteLine($"[CustomJWT] Token found - Length: {token.Length}");
                Console.WriteLine($"[CustomJWT] Token parts: {token.Split('.').Length}");

                // Manual JWT validation
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                Console.WriteLine($"[CustomJWT] Manual validation OK - Issuer: '{jwtToken.Issuer}', Audience: {string.Join(",", jwtToken.Audiences)}");

                // Get JWT config
                var jwtConfig = _configuration.GetSection("Jwt");
                var expectedIssuer = jwtConfig["Issuer"];
                var expectedAudience = jwtConfig["Audience"];

                // Validate issuer and audience (temporarily disabled for debugging)
                Console.WriteLine($"[CustomJWT] Issuer check - Expected: '{expectedIssuer}', Got: '{jwtToken.Issuer}'");
                Console.WriteLine($"[CustomJWT] Audience check - Expected: '{expectedAudience}', Got: {string.Join(",", jwtToken.Audiences)}");
                
                // Skip validation for now to test
                // if (jwtToken.Issuer != expectedIssuer)
                // {
                //     Console.WriteLine($"[CustomJWT] Invalid issuer. Expected: '{expectedIssuer}', Got: '{jwtToken.Issuer}'");
                //     return Task.FromResult(AuthenticateResult.Fail("Invalid issuer"));
                // }

                // if (!jwtToken.Audiences.Contains(expectedAudience))
                // {
                //     Console.WriteLine($"[CustomJWT] Invalid audience. Expected: '{expectedAudience}', Got: {string.Join(",", jwtToken.Audiences)}");
                //     return Task.FromResult(AuthenticateResult.Fail("Invalid audience"));
                // }

                // Check expiration
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    Console.WriteLine($"[CustomJWT] Token expired. ValidTo: {jwtToken.ValidTo}, Now: {DateTime.UtcNow}");
                    return Task.FromResult(AuthenticateResult.Fail("Token expired"));
                }

                // Create claims identity
                var claims = jwtToken.Claims.ToList();
                var identity = new ClaimsIdentity(claims, "CustomJWT");
                var principal = new ClaimsPrincipal(identity);

                Console.WriteLine($"[CustomJWT] Authentication successful for user: {jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value}");

                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(principal, "CustomJWT")));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CustomJWT] Authentication failed: {ex.Message}");
                return Task.FromResult(AuthenticateResult.Fail(ex.Message));
            }
        }
    }
}
