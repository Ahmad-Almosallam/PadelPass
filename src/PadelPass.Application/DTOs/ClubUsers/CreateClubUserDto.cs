using System.ComponentModel.DataAnnotations;
using PadelPass.Application.DTOs.Authentication;

namespace PadelPass.Application.DTOs.ClubUsers;

public class CreateClubUserDto
{
    [Required]
    public int ClubId { get; set; }
    
    // Either UserId or RegisterDto must be provided
    public string? UserId { get; set; }
    
    // New user information - used when creating a new user
    public RegisterDto? RegisterDto { get; set; }
}