using Microsoft.AspNetCore.Mvc;
using api_management.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Org.BouncyCastle.Bcpg;

namespace api_management
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;

        public AccountController(IAccountService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var (success, errorMessage) = await _service.RegisterAsync(model);
            if (success)
            {
                return Ok(new { success = true, message = "Registration successful!", VerifyEmail = model.IsVerifiedEmail });
            }
            return BadRequest(new { success = false, message = errorMessage });
        }

        [HttpGet("verify_email")]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            Console.WriteLine($"Verifying userId: {userId}, token: {token}");
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new { Error = "userId hoặc token không được để trống" });
            }

            try
            {
                var (succeeded, isVerifiedEmail, errorMessage) = await _service.VerifyEmailAsync(userId, token);
                if (!succeeded)
                {
                    return BadRequest(new { Error = errorMessage ?? "Xác minh thất bại" });
                }
                return Ok(new { IsVerifiedEmail = isVerifiedEmail }); // Trả về { "IsVerifiedEmail": true }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyEmail: {ex.Message}");
                return StatusCode(500, new { Error = "Lỗi server nội bộ" });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var token = await _service.LoginAsync(model);
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { success = false, message = "Invalid email or password" });
            }
            if (token == "Login Fail")
            {
                return Ok(new { success = false, message = "Login failed" });
            }
            return Ok(new { success = true, token = token });
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> LogOut(LogOutModel model)
        {
            var userId = model.Token;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { success = false, message = "User ID not found" });
            }

            var result = await _service.LogOutAsync(new LogOutModel { Token = userId });
            if (result)
            {
                return Ok(new { success = true, message = "Logout successful" });
            }
            return BadRequest(new { success = false, message = "Logout failed" });
        }

        [HttpPost("forgot_password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            var result = await _service.ForgotPasswordAsync(model.Email);
            return result.Succeeded
                ? Ok(new { success = true, message = "Password reset email sent successfully" })
                : BadRequest(new { success = false, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpPost("change_password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null)
                return Unauthorized();

            var result = await _service.ChangePasswordAsync(model);
            return result
                ? Ok(new { success = true, message = "Password changed successfully" })
                : BadRequest(new { success = false, message = "Failed to change password" });
        }
        [HttpPost("reset_password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest(new { success = false, message = "Invalid reset password data" });
            }

            var result = await _service.ResetPasswordAsync(model);
            if (result == "Password reset successful")
            {
                return Ok(new { success = true, message = result });
            }
            else
            {
                return BadRequest(new { success = false, message = result });
            }
        }
        [Authorize]
        [HttpGet("get_profile")]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _service.GetProfileAsync(email);
            if (user == null) return NotFound("Không tìm thấy người dùng");

            return Ok(new
            {
                user.UserName,
                user.PhoneNumber,
                user.SecurityStamp,
                user.TwoFactorEnabled
            });
        }
        [Authorize]
        [HttpPost("update_profile")]
        public async Task<IActionResult> ChangeProfile([FromQuery] UpdateProfileModel model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Invalid profile data" });
            }

            try
            {
                var result = await _service.UpdateProfileAsync(model);
                return Ok(new { success = true, message = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateProfile: {ex.Message}");
                return Ok(new { success = false, message = "Internal server error" });
            }
        }
    }
}