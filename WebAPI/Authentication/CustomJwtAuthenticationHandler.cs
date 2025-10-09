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
                if (!Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    return Task.FromResult(AuthenticateResult.NoResult());
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    return Task.FromResult(AuthenticateResult.Fail("Token expired"));
                }

                var claims = jwtToken.Claims.ToList();
                var identity = new ClaimsIdentity(claims, "CustomJWT");
                var principal = new ClaimsPrincipal(identity);

                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(principal, "CustomJWT")));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AuthenticateResult.Fail(ex.Message));
            }
        }
    }
}
