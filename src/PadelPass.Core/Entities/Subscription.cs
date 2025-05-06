using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelPass.Core.Entities;

public class Subscription : BaseEntity
{
    [Required] public string UserId { get; set; }

    [Required] public int PlanId { get; set; }

    [ForeignKey(nameof(PlanId))] public SubscriptionPlan Plan { get; set; }

    [Required] public DateTime StartDate { get; set; }

    [Required] public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
    
    // New properties for pause functionality
    public bool IsPaused { get; set; } = false;
    
    public DateTime? PauseDate { get; set; }
    
    public int? RemainingDays { get; set; }
}