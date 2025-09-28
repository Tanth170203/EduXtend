using BusinessObject.DTOs.GGLogin;
using BusinessObject.Models;
using DataAccess;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.GGLogin;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("google")]
        public async Task<ActionResult<TokenResponseDto>> GoogleLogin([FromBody] GoogleLoginRequest req)
        {
            var deviceInfo = Request.Headers.UserAgent.ToString();
            var token = await _auth.GoogleLoginAsync(req.IdToken, deviceInfo);
            if (token == null) return Unauthorized("Invalid Google token or domain is not allowed.");
            return Ok(new
            {
                Message = "Login successful",
                AccessToken = token.AccessToken,
                ExpiresAt = token.ExpiresAt,
                RefreshToken = token.RefreshToken
            });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequest req)
        {
            var deviceInfo = Request.Headers.UserAgent.ToString();
            var token = await _auth.RefreshAsync(req.RefreshToken, deviceInfo);
            if (token == null) return Unauthorized("Invalid refresh token.");
            return Ok(new
            {
                Message = "Refresh successful",
                AccessToken = token.AccessToken,
                ExpiresAt = token.ExpiresAt,
                RefreshToken = token.RefreshToken
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            await _auth.LogoutAsync(req.RefreshToken);
            return Ok(new { Message = "Logout successful" }); ;
        }
    }

}
