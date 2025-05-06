using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.Subscriptions;
using PadelPass.Application.Services;
using PadelPass.Core.Common;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Services.Implementations;

public class SubscriptionService 
{
    private readonly IGenericRepository<Subscription> _repository;
    private readonly IGenericRepository<SubscriptionPlan> _planRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IGenericRepository<Subscription> repository,
        IGenericRepository<SubscriptionPlan> planRepository,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<SubscriptionService> logger)
    {
        _repository = repository;
        _planRepository = planRepository;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<SubscriptionDto>> GetByIdAsync(int id)
    {
        try
        {
            var subscription = await _repository.AsQueryable(false)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (subscription == null)
            {
                return ApiResponse<SubscriptionDto>.Fail($"Subscription with ID {id} not found");
            }

            var dto = _mapper.Map<SubscriptionDto>(subscription);
            
            // Get user name
            var user = await _userManager.FindByIdAsync(subscription.UserId);
            if (user != null)
            {
                dto.UserName = user.FullName ?? user.UserName;
            }

            return ApiResponse<SubscriptionDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription with ID {SubscriptionId}", id);
            return ApiResponse<SubscriptionDto>.Fail("An error occurred while retrieving the subscription");
        }
    }
    
    public async Task<ApiResponse<SubscriptionDto>> GetCurrentUserSubscriptionAsync()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<SubscriptionDto>.Fail("User not authenticated");
            }
            
            var subscription = await _repository.AsQueryable(false)
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
                
            if (subscription == null)
            {
                return ApiResponse<SubscriptionDto>.Fail("No active subscription found");
            }

            var dto = _mapper.Map<SubscriptionDto>(subscription);
            
            // Get user name
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                dto.UserName = user.FullName ?? user.UserName;
            }

            return ApiResponse<SubscriptionDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user subscription");
            return ApiResponse<SubscriptionDto>.Fail("An error occurred while retrieving the current subscription");
        }
    }

    public async Task<ApiResponse<PaginatedList<SubscriptionDto>>> GetPaginatedAsync(
        int pageNumber, int pageSize, string orderBy = "StartDate", string orderType = "DESC")
    {
        try
        {
            var query = _repository.AsQueryable(false);

            query = query.Include(s => s.Plan);
            
            // If not admin, filter to current user only
            if (!_currentUserService.User.IsInRole("Admin"))
            {
                var userId = _currentUserService.UserId;
                query = query.Where(s => s.UserId == userId);
            }
            
            var paginatedResult = await _repository.GetPaginatedListAsync(
                query, pageNumber, pageSize, orderBy, orderType);
            
            var dtos = _mapper.Map<List<SubscriptionDto>>(paginatedResult.Items);
            
            // Get user names
            var userIds = dtos.Select(d => d.UserId).Distinct().ToList();
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName);
                
            foreach (var dto in dtos)
            {
                if (users.TryGetValue(dto.UserId, out var userName))
                {
                    dto.UserName = userName;
                }
            }
            
            var mappedResult = new PaginatedList<SubscriptionDto>(
                dtos,
                paginatedResult.TotalCount,
                paginatedResult.PageNumber,
                pageSize);
            
            return ApiResponse<PaginatedList<SubscriptionDto>>.Ok(mappedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated subscriptions");
            return ApiResponse<PaginatedList<SubscriptionDto>>.Fail("An error occurred while retrieving subscriptions");
        }
    }

    public async Task<ApiResponse<SubscriptionDto>> CreateAsync(CreateSubscriptionDto dto)
    {
        try
        {
            // Get current user
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<SubscriptionDto>.Fail("User not authenticated");
            }
            
            // Validate plan exists
            var plan = await _planRepository.GetByIdAsync(dto.PlanId);
            if (plan == null)
            {
                return ApiResponse<SubscriptionDto>.Fail($"Subscription plan with ID {dto.PlanId} not found");
            }
            
            // Check for existing active subscription
            var existingSubscription = await _repository.AsQueryable(false)
                .Where(s => s.UserId == userId && s.IsActive)
                .FirstOrDefaultAsync();
                
            if (existingSubscription != null)
            {
                return ApiResponse<SubscriptionDto>.Fail("User already has an active subscription");
            }
            
            // Create subscription
            var subscription = _mapper.Map<Subscription>(dto);
            subscription.UserId = userId;
            
            // Set start and end dates based on plan duration
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = subscription.StartDate.AddMonths(plan.DurationInMonths);
            
            _repository.Insert(subscription);
            
            // Update user's current subscription
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.CurrentSubscriptionId = subscription.Id;
                await _userManager.UpdateAsync(user);
            }
            
            await _repository.SaveChangesAsync();
            
            // Load the plan relationship for mapping
            subscription.Plan = plan;
            
            var entityDto = _mapper.Map<SubscriptionDto>(subscription);
            entityDto.UserName = user?.FullName ?? user?.UserName;
            
            return ApiResponse<SubscriptionDto>.Ok(entityDto, "Subscription created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return ApiResponse<SubscriptionDto>.Fail("An error occurred while creating the subscription");
        }
    }

    public async Task<ApiResponse<SubscriptionDto>> UpdateAsync(int id, UpdateSubscriptionDto dto)
    {
        try
        {
            var subscription = await _repository.AsQueryable(true)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (subscription == null)
            {
                return ApiResponse<SubscriptionDto>.Fail($"Subscription with ID {id} not found");
            }
            
            // If not admin, ensure user is updating their own subscription
            var userId = _currentUserService.UserId;
            if (!_currentUserService.User.IsInRole("Admin") && subscription.UserId != userId)
            {
                return ApiResponse<SubscriptionDto>.Fail("Unauthorized to update this subscription");
            }

            _mapper.Map(dto, subscription);
            
            _repository.Update(subscription);
            await _repository.SaveChangesAsync();
            
            var responseDto = _mapper.Map<SubscriptionDto>(subscription);
            
            // Get user name
            var user = await _userManager.FindByIdAsync(subscription.UserId);
            if (user != null)
            {
                responseDto.UserName = user.FullName ?? user.UserName;
            }
            
            return ApiResponse<SubscriptionDto>.Ok(responseDto, "Subscription updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription with ID {SubscriptionId}", id);
            return ApiResponse<SubscriptionDto>.Fail("An error occurred while updating the subscription");
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var subscription = await _repository.GetByIdAsync(id);
            if (subscription == null)
            {
                return ApiResponse<bool>.Fail($"Subscription with ID {id} not found");
            }
            
            // If not admin, ensure user is deleting their own subscription
            var userId = _currentUserService.UserId;
            if (!_currentUserService.User.IsInRole("Admin") && subscription.UserId != userId)
            {
                return ApiResponse<bool>.Fail("Unauthorized to delete this subscription");
            }

            _repository.Delete(subscription);
            await _repository.SaveChangesAsync();
            
            return ApiResponse<bool>.Ok(true, "Subscription deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription with ID {SubscriptionId}", id);
            return ApiResponse<bool>.Fail("An error occurred while deleting the subscription");
        }
    }
    
    public async Task<ApiResponse<bool>> CancelSubscriptionAsync(int id)
    {
        try
        {
            var subscription = await _repository.GetByIdAsync(id);
            if (subscription == null)
            {
                return ApiResponse<bool>.Fail($"Subscription with ID {id} not found");
            }
            
            // If not admin, ensure user is canceling their own subscription
            var userId = _currentUserService.UserId;
            if (!_currentUserService.User.IsInRole("Admin") && subscription.UserId != userId)
            {
                return ApiResponse<bool>.Fail("Unauthorized to cancel this subscription");
            }
            
            // Deactivate subscription
            subscription.IsActive = false;
            
            _repository.Update(subscription);
            
            // Update user's current subscription reference if needed
            var user = await _userManager.FindByIdAsync(subscription.UserId);
            if (user != null && user.CurrentSubscriptionId == id)
            {
                user.CurrentSubscriptionId = null;
                await _userManager.UpdateAsync(user);
            }
            
            await _repository.SaveChangesAsync();
            
            return ApiResponse<bool>.Ok(true, "Subscription canceled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription with ID {SubscriptionId}", id);
            return ApiResponse<bool>.Fail("An error occurred while canceling the subscription");
        }
    }
    
    
    public async Task<ApiResponse<SubscriptionDto>> ExtendSubscriptionAsync(ExtendSubscriptionDto dto)
    {
        try
        {
            var subscription = await _repository.AsQueryable(true)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);
                
            if (subscription == null)
            {
                return ApiResponse<SubscriptionDto>.Fail($"Subscription with ID {dto.Id} not found");
            }
            
            // If not admin, ensure user is extending their own subscription
            var userId = _currentUserService.UserId;
            if (!_currentUserService.User.IsInRole("Admin") && subscription.UserId != userId)
            {
                return ApiResponse<SubscriptionDto>.Fail("Unauthorized to extend this subscription");
            }
            
            // Check if subscription is active
            if (!subscription.IsActive)
            {
                return ApiResponse<SubscriptionDto>.Fail("Cannot extend an inactive subscription");
            }
            
            // Calculate new end date based on additional months
            DateTime newEndDate;
            
            if (subscription.IsPaused)
            {
                // For paused subscriptions, we add the months to the calculated end date
                int daysRemaining = subscription.RemainingDays ?? 0;
                newEndDate = DateTime.UtcNow.AddDays(daysRemaining).AddMonths(dto.AdditionalMonths);
                
                // Update the remaining days to include the extension
                subscription.RemainingDays = (int)(newEndDate - DateTime.UtcNow).TotalDays;
            }
            else
            {
                // For active subscriptions, we add months to the current end date
                newEndDate = subscription.EndDate.AddMonths(dto.AdditionalMonths);
                subscription.EndDate = newEndDate;
            }
            
            _repository.Update(subscription);
            await _repository.SaveChangesAsync();
            
            var responseDto = _mapper.Map<SubscriptionDto>(subscription);
            
            // Get user name
            var user = await _userManager.FindByIdAsync(subscription.UserId);
            if (user != null)
            {
                responseDto.UserName = user.FullName ?? user.UserName;
            }
            
            return ApiResponse<SubscriptionDto>.Ok(responseDto, "Subscription extended successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending subscription with ID {SubscriptionId}", dto.Id);
            return ApiResponse<SubscriptionDto>.Fail("An error occurred while extending the subscription");
        }
    }
    
    public async Task<ApiResponse<SubscriptionDto>> PauseSubscriptionAsync(int id)
    {
        try
        {
            var subscription = await _repository.AsQueryable(true)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (subscription == null)
            {
                return ApiResponse<SubscriptionDto>.Fail($"Subscription with ID {id} not found");
            }
            
            // If not admin, ensure user is pausing their own subscription
            var userId = _currentUserService.UserId;
            if (!_currentUserService.User.IsInRole("Admin") && subscription.UserId != userId)
            {
                return ApiResponse<SubscriptionDto>.Fail("Unauthorized to pause this subscription");
            }
            
            // Check if subscription is active
            if (!subscription.IsActive)
            {
                return ApiResponse<SubscriptionDto>.Fail("Cannot pause an inactive subscription");
            }
            
            // Check if subscription is already paused
            if (subscription.IsPaused)
            {
                return ApiResponse<SubscriptionDto>.Fail("Subscription is already paused");
            }
            
            // Calculate remaining days
            var now = DateTime.UtcNow;
            if (now > subscription.EndDate)
            {
                return ApiResponse<SubscriptionDto>.Fail("Subscription has already expired");
            }
            
            var remainingDays = (int)(subscription.EndDate - now).TotalDays;
            
            // Update subscription
            subscription.IsPaused = true;
            subscription.PauseDate = now;
            subscription.RemainingDays = remainingDays;
            
            _repository.Update(subscription);
            await _repository.SaveChangesAsync();
            
            var responseDto = _mapper.Map<SubscriptionDto>(subscription);
            
            // Get user name
            var user = await _userManager.FindByIdAsync(subscription.UserId);
            if (user != null)
            {
                responseDto.UserName = user.FullName ?? user.UserName;
            }
            
            return ApiResponse<SubscriptionDto>.Ok(responseDto, "Subscription paused successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing subscription with ID {SubscriptionId}", id);
            return ApiResponse<SubscriptionDto>.Fail("An error occurred while pausing the subscription");
        }
    }
    
    public async Task<ApiResponse<SubscriptionDto>> ResumeSubscriptionAsync(int id)
    {
        try
        {
            var subscription = await _repository.AsQueryable(true)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (subscription == null)
            {
                return ApiResponse<SubscriptionDto>.Fail($"Subscription with ID {id} not found");
            }
            
            // If not admin, ensure user is resuming their own subscription
            var userId = _currentUserService.UserId;
            if (!_currentUserService.User.IsInRole("Admin") && subscription.UserId != userId)
            {
                return ApiResponse<SubscriptionDto>.Fail("Unauthorized to resume this subscription");
            }
            
            // Check if subscription is active
            if (!subscription.IsActive)
            {
                return ApiResponse<SubscriptionDto>.Fail("Cannot resume an inactive subscription");
            }
            
            // Check if subscription is paused
            if (!subscription.IsPaused)
            {
                return ApiResponse<SubscriptionDto>.Fail("Subscription is not paused");
            }
            
            var remainingDays = subscription.RemainingDays ?? 0;
            if (remainingDays <= 0)
            {
                return ApiResponse<SubscriptionDto>.Fail("Subscription has no remaining days");
            }
            
            // Calculate new end date
            var now = DateTime.UtcNow;
            var newEndDate = now.AddDays(remainingDays);
            
            // Update subscription
            subscription.IsPaused = false;
            subscription.PauseDate = null;
            subscription.RemainingDays = null;
            subscription.EndDate = newEndDate;
            
            _repository.Update(subscription);
            await _repository.SaveChangesAsync();
            
            var responseDto = _mapper.Map<SubscriptionDto>(subscription);
            
            // Get user name
            var user = await _userManager.FindByIdAsync(subscription.UserId);
            if (user != null)
            {
                responseDto.UserName = user.FullName ?? user.UserName;
            }
            
            return ApiResponse<SubscriptionDto>.Ok(responseDto, "Subscription resumed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming subscription with ID {SubscriptionId}", id);
            return ApiResponse<SubscriptionDto>.Fail("An error occurred while resuming the subscription");
        }
    }
}
