using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelPass.Core.Entities;

public class CheckIn : BaseEntity
{
    [Required] public string UserId { get; set; }
    [ForeignKey(nameof(UserId))] public ApplicationUser User { get; set; }

    [Required] public int ClubId { get; set; }
    [ForeignKey(nameof(ClubId))] public Club Club { get; set; }

    [Required] public DateTimeOffset CheckInDateTime { get; set; }
    
    [StringLength(10)]
    public string CourtNumber { get; set; }
    
    public DateTimeOffset? StartPlayTime { get; set; }
    
    public TimeSpan? PlayDurationMinutes { get; set; }
    
    [StringLength(500)]
    public string Notes { get; set; }
    
    public bool IsManualEntry { get; set; } = false;
    
    [StringLength(50)]
    public string CheckedInBy { get; set; } // Staff member who processed check-in
}