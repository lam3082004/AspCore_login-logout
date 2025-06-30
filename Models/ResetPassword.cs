using System.ComponentModel.DataAnnotations;

namespace api_management.Models;

public class ResetPasswordModel
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}
