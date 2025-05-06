using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.Subscriptions;

public class ExtendSubscriptionDto
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [Range(1, 36)]
    public int AdditionalMonths { get; set; }
}