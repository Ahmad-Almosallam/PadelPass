using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.Authentication;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
}