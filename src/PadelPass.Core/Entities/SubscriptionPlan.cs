using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelPass.Core.Entities;

public class SubscriptionPlan : BaseEntity
{
    [Required] [StringLength(100)] public string Name { get; set; }

    [Range(1, 36)] public int DurationInMonths { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}