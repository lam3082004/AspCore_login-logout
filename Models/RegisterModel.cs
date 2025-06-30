using System.ComponentModel.DataAnnotations;

namespace api_management.Models
{
    public class RegisterModel
    {
        [Required]
        public string UserName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        // [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        //     ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
        public string Password { get; set; }
        [Required]
        public string ConfirmPassword { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsVerifiedEmail { get; set; } = false;
    }    
}
