using System.ComponentModel.DataAnnotations;

namespace api_management.Models{
    public class ChangePasswordModel
    {
        public string email { get; set; }
        [Required]
        public string CurrentPassword { get; set; }
        [Required]

        public string NewPassword { get; set; }

    }
}