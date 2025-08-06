using PadelPass.Core.Entities;

namespace PadelPass.Core.Services;

public interface ITimeZoneService
{
    DateTimeOffset ConvertToClubTime(DateTimeOffset utcDateTime, string clubTimeZoneId);
    DateTimeOffset ConvertToUtc(DateTimeOffset clubDateTime, string clubTimeZoneId);
    DateTimeOffset GetClubNow(string clubTimeZoneId);
    bool IsWithinNonPeakHours(DateTimeOffset clubDateTime, IEnumerable<NonPeakSlot> nonPeakSlots);
    string FormatForClub(DateTimeOffset dateTime, string clubTimeZoneId, string format = "yyyy-MM-dd HH:mm");
}