using PadelPass.Core.Entities;
using PadelPass.Core.Services;

namespace PadelPass.Infrastructure.Services;

public class TimeZoneService : ITimeZoneService
{
    public DateTimeOffset ConvertToClubTime(DateTimeOffset utcDateTime, string clubTimeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clubTimeZoneId);
        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }

    public DateTimeOffset ConvertToUtc(DateTimeOffset clubDateTime, string clubTimeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(clubTimeZoneId);
        return TimeZoneInfo.ConvertTimeToUtc(clubDateTime.DateTime, timeZone);
    }

    public DateTimeOffset GetClubNow(string clubTimeZoneId)
    {
        return ConvertToClubTime(DateTimeOffset.UtcNow, clubTimeZoneId);
    }

    public bool IsWithinNonPeakHours(DateTimeOffset clubDateTime, IEnumerable<NonPeakSlot> nonPeakSlots)
    {
        var dayOfWeek = clubDateTime.DayOfWeek;
        var timeOfDay = clubDateTime.TimeOfDay;

        return nonPeakSlots.Any(slot =>
            slot.DayOfWeek == dayOfWeek &&
            timeOfDay >= slot.StartTime &&
            timeOfDay <= slot.EndTime);
    }

    public string FormatForClub(DateTimeOffset dateTime, string clubTimeZoneId, string format = "yyyy-MM-dd HH:mm")
    {
        var clubTime = ConvertToClubTime(dateTime, clubTimeZoneId);
        return clubTime.ToString(format);
    }
}