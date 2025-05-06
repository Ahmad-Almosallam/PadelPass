using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.Authentication;

public class CreateAdminDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FullName { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 6)]
    public string Password { get; set; }
}