using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PadelPass.Application.DTOs.ClubUsers;
using PadelPass.Application.Services.Implementations;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Shared;

namespace PadelPass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClubUsersController : ControllerBase
{
    private readonly ClubUserService _clubUserService;

    public ClubUsersController(ClubUserService clubUserService)
    {
        _clubUserService = clubUserService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin},{AppRoles.ClubUser}")]
    public async Task<ActionResult<ApiResponse<PaginatedList<ClubUserDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? clubId = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] string orderType = "ASC")
    {
        var result = await _clubUserService.GetPaginatedAsync(pageNumber, pageSize, clubId, orderBy, orderType);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin},{AppRoles.ClubUser}")]
    public async Task<ActionResult<ApiResponse<ClubUserDto>>> GetById(int id)
    {
        var result = await _clubUserService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }
    
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<ClubUserDto>>> Create([FromBody] CreateClubUserDto dto)
    {
        var result = await _clubUserService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<ClubUserDto>>> Update(int id, [FromBody] UpdateClubUserDto dto)
    {
        var result = await _clubUserService.UpdateAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _clubUserService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("search-users")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin},{AppRoles.ClubUser}")]
    public async Task<ActionResult<ApiResponse<PaginatedList<UserSearchDto>>>> SearchUsers(
        [FromQuery] string searchTerm = null,
        [FromQuery] int? clubId = null,
        [FromQuery] bool? hasActiveSubscription = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new UserSearchQueryDto
        {
            SearchTerm = searchTerm,
            ClubId = clubId,
            HasActiveSubscription = hasActiveSubscription,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _clubUserService.SearchUsersAsync(query);
        return Ok(result);
    }

    [HttpGet("user-details/{phoneNumber}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin},{AppRoles.ClubUser}")]
    public async Task<ActionResult<ApiResponse<UserSearchDto>>> GetUserDetails(string phoneNumber)
    {
        var result = await _clubUserService.GetUserWithSubscriptionDetailsAsync(phoneNumber);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }
    
    [HttpGet("check-subscription-by-phone")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin},{AppRoles.ClubUser}")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckSubscriptionByPhone([FromQuery] string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return BadRequest(ApiResponse<bool>.Fail("Phone number is required"));
        }

        var result = await _clubUserService.CheckUserSubscriptionByPhoneAsync(phoneNumber);
        return Ok(result);
    }
}