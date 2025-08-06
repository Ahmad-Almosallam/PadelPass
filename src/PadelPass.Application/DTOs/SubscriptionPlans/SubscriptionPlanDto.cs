namespace PadelPass.Application.DTOs.SubscriptionPlans;

public class SubscriptionPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int DurationInMonths { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}