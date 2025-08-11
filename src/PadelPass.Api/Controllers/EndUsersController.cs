using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PadelPass.Application.DTOs.ClubUsers;
using PadelPass.Application.Services.Implementations;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Shared;

namespace PadelPass.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EndUsersController : ControllerBase
    {
        private readonly EndUserService _endUserService;

        public EndUsersController(EndUserService endUserService)
        {
            _endUserService = endUserService;
        }

        [HttpPost("SearchUsersAsync")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin},{AppRoles.ClubUser}")]
        public async Task<ActionResult<ApiResponse<PaginatedList<UserSearchDto>>>> SearchUsersAsync([FromBody] UserSearchQueryDto query)
        {
            var result = await _endUserService.SearchUsersAsync(query);
            return Ok(result);
        }
    }
}

