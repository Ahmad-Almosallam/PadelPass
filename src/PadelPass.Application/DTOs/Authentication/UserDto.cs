namespace PadelPass.Application.DTOs.Authentication;

public class UserDto
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string FullName { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}