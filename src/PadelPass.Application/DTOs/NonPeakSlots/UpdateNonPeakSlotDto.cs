using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.NonPeakSlots;

public class UpdateNonPeakSlotDto
{
    [Range(0, 6)]
    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }
}