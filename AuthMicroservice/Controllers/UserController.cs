using AuthMicroservice.Application.Dtos;
using System.Security.Claims;
using AuthMicroservice.Application.Interface;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace AuthMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        [HttpGet]
        public async Task<ActionResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost("Register")]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid input data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                var createdUser = await _userService.CreateAsync(userDto);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.UserId }, createdUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid input data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                await _userService.UpdateAsync(id, userDto);
                return Ok(new { Message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                return Ok(new { Message = $"User with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var (accessToken, refreshToken) = await _userService.LoginAsync(loginDto);

                var accessCookieOption = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    MaxAge = TimeSpan.FromHours(1)
                };
                Response.Cookies.Append("access_token", accessToken, accessCookieOption);

                var refreshCookieOption = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOption);

                return Ok(new { access_token = accessToken, refresh_token = refreshToken, message = "Login successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpGet("login-google")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("callback")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                if (!result.Succeeded)
                    return Unauthorized(new { Message = "Google authentication failed" });

                var (accessToken, oAuthUser) = await _userService.HandleGoogleCallbackAsync(result.Ticket);

                var cookieOption = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    MaxAge = TimeSpan.FromHours(1)
                };
                Response.Cookies.Append("access_token", accessToken, cookieOption);

                return Redirect("http://localhost:3000/Dashboard?googleLogin=true");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("me")]


        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var identity = HttpContext.User.Identity as ClaimsIdentity;
                var user = await _userService.GetCurrentUserAsync(identity);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var refreshToken = Request.Cookies["refresh_token"];
                await _userService.LogoutAsync(refreshToken);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(-1)
                };

                Response.Cookies.Delete("access_token", cookieOptions);
                Response.Cookies.Delete("refresh_token", cookieOptions);

                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refresh_token"];
                var (accessToken, newRefreshToken) = await _userService.RefreshTokenAsync(refreshToken);

                Response.Cookies.Append("access_token", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    MaxAge = TimeSpan.FromHours(1)
                });

                Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                return Ok(new { access_token = accessToken });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("OtpVerify")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpDto dto)
        {
            try
            {
                var (accessToken, refreshToken) = await _userService.VerifyOtpAndGenerateJwt(dto);

                var accessCookieOption = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    MaxAge = TimeSpan.FromHours(1)
                };
                Response.Cookies.Append("access_token", accessToken, accessCookieOption);

                var refreshCookieOption = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOption);
                return Ok(new { access_token = accessToken, refresh_token = refreshToken, message = "Login successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }


        }

    }
}
