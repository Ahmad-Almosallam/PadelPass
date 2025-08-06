using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.CheckIns;

public class CreateCheckInDto
{
    [Required]
    [Phone]
    public string UserPhoneNumber { get; set; }
    
    [Required]
    public int ClubId { get; set; }
    
    public DateTimeOffset? CheckInDateTime { get; set; } = DateTimeOffset.UtcNow;
    
    [StringLength(10)]
    public string CourtNumber { get; set; }
    
    public DateTimeOffset? StartPlayTime { get; set; }
    
    public TimeSpan? PlayDurationMinutes { get; set; }
    
    [StringLength(500)]
    public string Notes { get; set; }
}