using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PadelPass.Application.DTOs.NonPeakSlots;
using PadelPass.Application.Services;
using PadelPass.Application.Services.Implementations;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Shared;

namespace PadelPass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NonPeakSlotsController : ControllerBase
{
    private readonly NonPeakSlotService _nonPeakSlotService;

    public NonPeakSlotsController(
        NonPeakSlotService nonPeakSlotService)
    {
        _nonPeakSlotService = nonPeakSlotService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<NonPeakSlotDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? clubId = null,
        [FromQuery] string orderBy = "DayOfWeek",
        [FromQuery] string orderType = "ASC")
    {
        var result = await _nonPeakSlotService.GetPaginatedAsync(pageNumber, pageSize, clubId, orderBy, orderType);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NonPeakSlotDto>>> GetById(
        int id)
    {
        var result = await _nonPeakSlotService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("club/{clubId}")]
    public async Task<ActionResult<ApiResponse<PaginatedList<NonPeakSlotDto>>>> GetByClubId(
        int clubId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string orderBy = "DayOfWeek",
        [FromQuery] string orderType = "ASC")
    {
        var result = await _nonPeakSlotService.GetPaginatedAsync(pageNumber, pageSize, clubId, orderBy, orderType);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<NonPeakSlotDto>>> Create(
        [FromBody] CreateNonPeakSlotDto dto)
    {
        var result = await _nonPeakSlotService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<NonPeakSlotDto>>> Update(
        int id,
        [FromBody] UpdateNonPeakSlotDto dto)
    {
        var result = await _nonPeakSlotService.UpdateAsync(id, dto);
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
        var result = await _nonPeakSlotService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}