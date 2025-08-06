namespace PadelPass.Application.DTOs.Subscriptions;

public class SubscriptionDto
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; }
    public decimal PlanPrice { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }
    public DateTimeOffset? PauseDate { get; set; }
    public int? RemainingDays { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}