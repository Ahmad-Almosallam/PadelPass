using System.ComponentModel.DataAnnotations;

namespace PadelPass.Core.Entities;

public class RefreshToken : BaseEntity
{
    [Required]
    public string UserId { get; set; }
    
    [Required]
    public string Token { get; set; }
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    public bool IsRevoked { get; set; }
    
    public bool IsUsed { get; set; }
    
    public string JwtId { get; set; }
}