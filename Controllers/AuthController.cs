using DDACAssignment.Dtos.TokenRequest;
using DDACAssignment.Dtos.User;
using DDACAssignment.Models;
using DDACAssignment.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace DDACAssignment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService, IWebHostEnvironment env) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(AuthDto request)
        {
            var user = await authService.RegisterAsync(request);

            if (user is null)
                return BadRequest("Username is already exists!");

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(AuthDto request)
        {
            var result = await authService.LoginAsync(request);
            if (result is null)
                return BadRequest("Invalid username or password");

            // Set refresh token as an HttpOnly cookie so it's not accessible from JS
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // Use secure cookies in non-development environments (should be true for production HTTPS)
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            if (result.RefreshToken is not null)
            {
                Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);
            }

            // Return access token only in the response body — refresh token is stored in HttpOnly cookie
            return Ok(new { accessToken = result.AccessToken, role = result.Role });
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokenAsync(request);

            if (result is null)
                return Unauthorized("Invalid Refresh Token");

            if (result.AccessToken is null)
                return Unauthorized("Invalid Refresh Token");

            if (result.RefreshToken is null)
                return Unauthorized("Invalid Refresh Token");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            if (result.RefreshToken is not null)
            {
                Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);
            }

            return Ok(new { accessToken = result.AccessToken, role = result.Role });
        }
    }
}
