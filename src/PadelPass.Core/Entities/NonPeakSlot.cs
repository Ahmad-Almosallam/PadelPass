using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelPass.Core.Entities;

public class NonPeakSlot : BaseEntity
{
    [Required] public int ClubId { get; set; }

    [ForeignKey(nameof(ClubId))] public Club Club { get; set; }

    [Required] [Range(0, 6)] public DayOfWeek DayOfWeek { get; set; }

    [Required] public TimeSpan StartTime { get; set; }

    [Required] public TimeSpan EndTime { get; set; }
}