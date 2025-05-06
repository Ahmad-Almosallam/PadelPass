using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.SubscriptionPlans;

public class CreateSubscriptionPlanDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [Range(1, 36)]
    public int DurationInMonths { get; set; }

    [Required]
    [Range(0.01, 10000)]
    public decimal Price { get; set; }
}