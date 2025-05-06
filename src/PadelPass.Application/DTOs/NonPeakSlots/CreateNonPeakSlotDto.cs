using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.NonPeakSlots;

public class CreateNonPeakSlotDto
{
    [Required]
    public int ClubId { get; set; }

    [Required]
    [Range(0, 6)]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }
}