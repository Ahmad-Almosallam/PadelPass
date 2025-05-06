namespace PadelPass.Application.DTOs.Subscriptions;

public class SubscriptionDto
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; }
    public decimal PlanPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }
    public DateTime? PauseDate { get; set; }
    public int? RemainingDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}