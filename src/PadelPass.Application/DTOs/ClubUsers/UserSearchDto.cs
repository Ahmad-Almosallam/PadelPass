﻿namespace PadelPass.Application.DTOs.ClubUsers;

public class UserSearchDto
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public bool HasActiveSubscription { get; set; }
    public string SubscriptionPlanName { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
}