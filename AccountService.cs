using api_management.Data;
using api_management.Models;
using api_management;
using Microsoft.AspNetCore.Identity;
using api_management.helper;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Web;
public interface IAccountService
{
    Task<(bool Succeeded, string ErrorMessage)> RegisterAsync(RegisterModel model);
    Task<(bool Succeeded, bool IsVerifiedEmail, string ErrorMessage)> VerifyEmailAsync(string userId, string token);
    Task<string> LoginAsync(LoginModel model);
    Task<bool> LogOutAsync(LogOutModel model);
    Task<(bool Succeeded, string ErrorMessage)> ForgotPasswordAsync(string email);
    Task<bool> ChangePasswordAsync(ChangePasswordModel model);
    Task<string> ResetPasswordAsync(ResetPasswordModel model);
    Task<ApplicationUser> GetProfileAsync(string email);
    Task<string> UpdateProfileAsync(UpdateProfileModel user);
}

public class AccountService(IAccountRepository accountRepository, UserManager<ApplicationUser> userManager, EmailSender emailSender) : IAccountService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly EmailSender _emailSender = emailSender;

    // AccountService - Phần RegisterAsync và VerifyEmailAsync đã sửa
    public async Task<(bool Succeeded, string ErrorMessage)> RegisterAsync(RegisterModel model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
        {
            return (false, string.Join("; ", validationResults.Select(r => r.ErrorMessage)));
        }

        if (model.Password != model.ConfirmPassword)
        {
            return (false, "Mật khẩu xác nhận không khớp");
        }

        if (!await _accountRepository.IsEmailUniqueAsync(model.Email) && !string.IsNullOrEmpty(model.Email))
        {
            return (false, "Email đã được sử dụng hoặc không hợp lệ");
        }

        var result = await _accountRepository.CreateUserAsync(model);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // Gửi email xác nhận
        var user = await _accountRepository.GetUserByEmailAsync(model.Email);
        if (user != null)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // Sử dụng Base64 encoding thay vì UrlEncode để tránh lỗi token
            var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));
            var confirmationLink = $"http://localhost:5194/api/Account/verify_email?userId={user.Id}&token={encodedToken}";
            await _emailSender.SendVerificationEmailAsync(model.Email, confirmationLink, true);
        }

        return (true, "Đăng ký thành công");
    }

    public async Task<(bool Succeeded, bool IsVerifiedEmail, string ErrorMessage)> VerifyEmailAsync(string userId, string token)
    {
        try
        {
            Console.WriteLine($"Verifying user: {userId}, token length: {token?.Length}");

            var user = await _accountRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return (false, false, "Người dùng không tồn tại");
            }

            // Kiểm tra xem email đã được xác nhận chưa
            if (user.EmailConfirmed)
            {
                return (true, true, "Email đã được xác nhận trước đó");
            }

            // Decode token từ Base64
            string decodedToken;
            try
            {
                var tokenBytes = Convert.FromBase64String(token);
                decodedToken = System.Text.Encoding.UTF8.GetString(tokenBytes);
            }
            catch (FormatException)
            {
                // Nếu không phải Base64, thử UrlDecode (để tương thích với token cũ)
                decodedToken = HttpUtility.UrlDecode(token);
            }

            Console.WriteLine($"Decoded token length: {decodedToken?.Length}");

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"ConfirmEmailAsync failed: {errors}");
                return (false, false, $"Xác minh thất bại: {errors}");
            }

            // Cập nhật user để đảm bảo EmailConfirmed = true
            await _accountRepository.UpdateUserAsync(user);

            return (true, true, "Xác minh email thành công");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Verification error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return (false, false, $"Lỗi: {ex.Message}");
        }
    }
    public async Task<string> LoginAsync(LoginModel model)
    {
        var token = await _accountRepository.LoginAsync(model);
        if (string.IsNullOrEmpty(token))
        {
            return "Loginfail"; // Trả về null nếu đăng nhập không thành công;
        }
        return token;
    }
    public async Task<bool> LogOutAsync(LogOutModel model)
    {
        return await Task.FromResult(true); // Giả sử đăng xuất thành công
    }
    public async Task<(bool Succeeded, string ErrorMessage)> ForgotPasswordAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Email không được để trống");
        }

        var user = await _accountRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            return (false, "Email không tồn tại");
        }

        if (!user.EmailConfirmed)
        {
            return (false, "Email chưa được xác minh");
        }

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var resetLink = $"http://localhost:5194/api/Account/reset_password?userId={user.Id}&token={encodedToken}";
            await _emailSender.SendVerificationEmailAsync(email, resetLink, false);
            return (true, "Email đặt lại mật khẩu đã được gửi");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi gửi email: {ex.Message}");
        }
    }
    public async Task<bool> ChangePasswordAsync(ChangePasswordModel model)
    {
        var result = await _accountRepository.ChangePasswordAsync(model);
        if (!result.Succeeded)
        {
            Console.WriteLine($"ChangePasswordAsync failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            return false; // Trả về false nếu thay đổi mật khẩu không thành công
        }
        return true;
    }
    public async Task<string> ResetPasswordAsync(ResetPasswordModel model)
        {
            var result = await _accountRepository.ResetPasswordAsync(model);
            return result.Succeeded
                ? "Password reset successful"
                : string.Join("; ", result.Errors.Select(e => e.Description));
        }
    public async Task<ApplicationUser> GetProfileAsync(string email)
    {
        var user = await _accountRepository.GetUserByEmailAsync(email) ?? throw new KeyNotFoundException("Người dùng không tồn tại");
        return user;
    }
    public async Task<string> UpdateProfileAsync(UpdateProfileModel user)
    {
        return await _accountRepository.UpdateProfileAsync(user);
    }
}