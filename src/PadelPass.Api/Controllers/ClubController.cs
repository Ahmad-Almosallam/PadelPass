using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PadelPass.Application.DTOs.Clubs;
using PadelPass.Application.Services;
using PadelPass.Application.Services.Implementations;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Shared;

namespace PadelPass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClubsController : ControllerBase
{
    private readonly ClubService _clubService;

    public ClubsController(
        ClubService clubService)
    {
        _clubService = clubService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<ClubDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string orderBy = "Name",
        [FromQuery] string orderType = "ASC")
    {
        var result = await _clubService.GetPaginatedAsync(pageNumber, pageSize, orderBy, orderType);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ClubDto>>> GetById(
        int id)
    {
        var result = await _clubService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<ClubDto>>> Create(
        [FromBody] CreateClubDto dto)
    {
        var result = await _clubService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<ClubDto>>> Update(
        int id,
        [FromBody] UpdateClubDto dto)
    {
        var result = await _clubService.UpdateAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id)
    {
        var result = await _clubService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}