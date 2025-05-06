using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PadelPass.Core.Services;

namespace PadelPass.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal User => _httpContextAccessor?.HttpContext?.User;
    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string UserName => User?.Identity?.Name;
}