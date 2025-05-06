using System.Security.Claims;

namespace PadelPass.Core.Services;

public interface ICurrentUserService
{
    ClaimsPrincipal User { get; }
    public string UserId { get; }
    public string UserName { get; }
}