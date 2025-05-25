namespace PadelPass.Application.DTOs.ClubUsers;

public class ClubUserDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}