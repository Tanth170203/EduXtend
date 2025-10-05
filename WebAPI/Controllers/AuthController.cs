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
            try
            {
                if (string.IsNullOrEmpty(req.IdToken))
                    return BadRequest("ID Token is required.");

                var deviceInfo = Request.Headers.UserAgent.ToString();
                var token = await _auth.GoogleLoginAsync(req.IdToken, deviceInfo);
                
                if (token == null) 
                    return Unauthorized("Invalid Google token or domain is not allowed.");
                
                return Ok(new
                {
                    Message = "Login successful",
                    AccessToken = token.AccessToken,
                    ExpiresAt = token.ExpiresAt,
                    RefreshToken = token.RefreshToken
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Google login error: {ex.Message}");
                return StatusCode(500, "Internal server error during login.");
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.RefreshToken))
                    return BadRequest("Refresh token is required.");

                var deviceInfo = Request.Headers.UserAgent.ToString();
                var token = await _auth.RefreshAsync(req.RefreshToken, deviceInfo);
                
                if (token == null) 
                    return Unauthorized("Invalid refresh token.");
                
                return Ok(new
                {
                    Message = "Refresh successful",
                    AccessToken = token.AccessToken,
                    ExpiresAt = token.ExpiresAt,
                    RefreshToken = token.RefreshToken
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token refresh error: {ex.Message}");
                return StatusCode(500, "Internal server error during token refresh.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.RefreshToken))
                    return BadRequest("Refresh token is required.");

                await _auth.LogoutAsync(req.RefreshToken);
                return Ok(new { Message = "Logout successful" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                return StatusCode(500, "Internal server error during logout.");
            }
        }
    }

}
