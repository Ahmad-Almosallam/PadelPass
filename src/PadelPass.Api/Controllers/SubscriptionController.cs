using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PadelPass.Application.DTOs.Subscriptions;
using PadelPass.Application.Services;
using PadelPass.Application.Services.Implementations;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Shared;

namespace PadelPass.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionsController(
        SubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<SubscriptionDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string orderBy = "StartDate",
        [FromQuery] string orderType = "DESC")
    {
        var result = await _subscriptionService.GetPaginatedAsync(pageNumber, pageSize, orderBy, orderType);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> GetById(
        int id)
    {
        var result = await _subscriptionService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("current")]
    [Authorize(AppRoles.User)]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> GetCurrentUserSubscription()
    {
        var result = await _subscriptionService.GetCurrentUserSubscriptionAsync();
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> Create(
        [FromBody] CreateSubscriptionDto dto)
    {
        var result = await _subscriptionService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> Update(
        int id,
        [FromBody] UpdateSubscriptionDto dto)
    {
        var result = await _subscriptionService.UpdateAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> Cancel(
        int id)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id)
    {
        var result = await _subscriptionService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
    
    [HttpPost("extend")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> Extend([FromBody] ExtendSubscriptionDto dto)
    {
        var result = await _subscriptionService.ExtendSubscriptionAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
    
    [HttpPost("{id:int}/pause")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> Pause(int id)
    {
        var result = await _subscriptionService.PauseSubscriptionAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
    
    [HttpPost("{id:int}/resume")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> Resume(int id)
    {
        var result = await _subscriptionService.ResumeSubscriptionAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}