using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using api_management.Data;
using api_management.Models;
using System;
using System.Threading.Tasks;
using System.Web;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace api_management
{
    // Interface và implementation gộp chung 1 file
    public interface IAccountRepository
    {
        Task<bool> IsEmailUniqueAsync(string email);
        Task<IdentityResult> CreateUserAsync(RegisterModel model);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task UpdateUserAsync(ApplicationUser user);
        Task<ApplicationUser> GetUserByIdAsync(string id);
        Task<string> LoginAsync(LoginModel model);
        Task<bool> LogOutAsync(string userId);
        Task<IdentityResult> ChangePasswordAsync(ChangePasswordModel model);
        Task<string> ConfirmEmailAsync(string userId, string token);
        Task<string> GetEmailByUserIdAsync(string userId);
        Task<ApplicationUser> GetProfileAsync(string userId);
        Task<string> UpdateProfileAsync(UpdateProfileModel user);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordModel model);
    }

    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        public AccountRepository(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            var user = await _userManager.FindByEmailAsync(email);
            return user == null;
        }

        public async Task<IdentityResult> CreateUserAsync(RegisterModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email, // Sử dụng Email làm UserName để tránh xung đột
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            // Chỉ set EmailConfirmed = true nếu model.IsVerifiedEmail = true
            if (result.Succeeded)
            {
                await _userManager.UpdateAsync(user);
            }
            if (model.IsVerifiedEmail)
            {
                user.EmailConfirmed = true;
            }

            if (result.Succeeded)
            {
                await AssignRoleAsync(user, "Member");
            }

            return result;
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }
        public async Task<ApplicationUser> GetUserByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }
        public async Task UpdateUserAsync(ApplicationUser user)
        {
            await _userManager.UpdateAsync(user);
        }
        public async Task<string> GetEmailByUserIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.Email;
        }

        public async Task AssignRoleAsync(ApplicationUser user, string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole { Name = role, NormalizedName = role.ToUpper() });
            }
            await _userManager.AddToRoleAsync(user, role);
        }
        public async Task<string> LoginAsync(LoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return "Login Fail";
                }

                // Tạo danh sách claim cho token
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id), // Thêm UserId vào claim
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Lấy các role của user và thêm vào claim
                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                }

                // Nếu người dùng chọn nhớ đăng nhập thì token sẽ có thời gian sống lâu hơn
                var tokenExpiry = model.RememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(2);

                var authenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    expires: tokenExpiry,
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authenKey, SecurityAlgorithms.HmacSha256)
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            return "Login Fail";
        }
        public async Task<bool> LogOutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            await _signInManager.SignOutAsync();
            return true; // Giả sử đăng xuất thành công
        }
        public async Task<IdentityResult> ChangePasswordAsync(ChangePasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.email);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            if (user.PasswordHash == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "P" });
            }
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                Console.WriteLine($"ChangePasswordAsync failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
            return result;
        }
        public async Task<string> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return "User not found";

            // Giải mã token từ Base64
            var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                return "Xác minh email thành công";
            }
            else
            {
                return "Xác minh email thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var decodedToken = HttpUtility.UrlDecode(model.Token);
            return await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);//Identity: đặt lại mật khẩu
        }
        public async Task<ApplicationUser> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("Người dùng không tồn tại");
            return user;
        }
        public async Task<string> UpdateProfileAsync(UpdateProfileModel user)
        {
            var existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser == null) return "Người dùng không tồn tại";

            existingUser.UserName = user.New_UserName;
            existingUser.PhoneNumber = user.New_PhoneNumber;
            // _userManager.ChangePhoneNumberAsync()
            var result = await _userManager.UpdateAsync(existingUser);
            if (result.Succeeded)
            {
                return "Cập nhật thành công";
            }
            else
            {
                return "Cập nhật thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
    }
}