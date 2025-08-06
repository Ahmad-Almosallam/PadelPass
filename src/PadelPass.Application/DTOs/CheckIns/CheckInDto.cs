namespace PadelPass.Application.DTOs.CheckIns;

public class CheckInDto
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string UserPhoneNumber { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; }
    public DateTimeOffset CheckInDateTime { get; set; } // Will be in club's timezone for display
    public string CourtNumber { get; set; }
    public DateTimeOffset? StartPlayTime { get; set; }
    public int? PlayDurationMinutes { get; set; }
    public string Notes { get; set; }
    public bool IsManualEntry { get; set; }
    public string CheckedInBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    // Additional display properties
    public string FormattedCheckInTime { get; set; }
    public string TimeZone { get; set; }
}