namespace PadelPass.Application.DTOs.NonPeakSlots;

public class NonPeakSlotDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}