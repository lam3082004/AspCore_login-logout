using System.ComponentModel.DataAnnotations;

namespace api_management.Models
{
    public class ForgotPasswordModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}