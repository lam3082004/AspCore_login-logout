using System.ComponentModel.DataAnnotations;
using MimeKit.Cryptography;

public class UpdateProfileModel
{
    [Required]
    public string Email { get; set; }
    // public string UserName { get; set; }
    public string New_UserName { get; set; }

    // public string PhoneNumber { get; set; }
    public string New_PhoneNumber { get; set; }

}