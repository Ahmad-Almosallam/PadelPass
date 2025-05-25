namespace PadelPass.Application.DTOs.ClubUsers;

public class UserSearchQueryDto
{
    public string SearchTerm { get; set; }
    public int? ClubId { get; set; }
    public bool? HasActiveSubscription { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}