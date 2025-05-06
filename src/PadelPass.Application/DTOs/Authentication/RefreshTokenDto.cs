using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.Authentication;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; }
}