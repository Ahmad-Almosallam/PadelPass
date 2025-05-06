using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PadelPass.Application.DTOs.SubscriptionPlans;
using PadelPass.Application.Services;
using PadelPass.Application.Services.Implementations;
using PadelPass.Core.Common;
using PadelPass.Core.Shared;

namespace PadelPass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly SubscriptionPlanService _subscriptionPlanService;

    public SubscriptionPlansController(
        SubscriptionPlanService subscriptionPlanService)
    {
        _subscriptionPlanService = subscriptionPlanService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<SubscriptionPlanDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string orderBy = "Name",
        [FromQuery] string orderType = "ASC")
    {
        var result = await _subscriptionPlanService.GetPaginatedAsync(pageNumber, pageSize, orderBy, orderType);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SubscriptionPlanDto>>> GetById(
        int id)
    {
        var result = await _subscriptionPlanService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SubscriptionPlanDto>>> Create(
        [FromBody] CreateSubscriptionPlanDto dto)
    {
        var result = await _subscriptionPlanService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SubscriptionPlanDto>>> Update(
        int id,
        [FromBody] UpdateSubscriptionPlanDto dto)
    {
        var result = await _subscriptionPlanService.UpdateAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id)
    {
        var result = await _subscriptionPlanService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}