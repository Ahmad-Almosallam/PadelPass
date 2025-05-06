using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.Subscriptions;

public class CreateSubscriptionDto
{
    [Required]
    public int PlanId { get; set; }
    
    // UserId will be set by the CurrentUserService
}