namespace PadelPass.Application.DTOs.Authentication;

public class AuthResponseDto
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}