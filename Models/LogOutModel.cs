using System.ComponentModel.DataAnnotations;

public class LogOutModel
{
    [Required]
    public string Token { get; set; }
}