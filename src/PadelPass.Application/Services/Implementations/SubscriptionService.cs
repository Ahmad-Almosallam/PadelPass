using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.Subscriptions;
using PadelPass.Application.Services;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Services.Implementations
{
    public class SubscriptionService
    {
        private readonly IGenericRepository<Subscription> _repository;
        private readonly IGenericRepository<SubscriptionPlan> _planRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<SubscriptionService> _logger;
        private readonly IGlobalLocalizer _localizer;

        public SubscriptionService(
            IGenericRepository<Subscription> repository,
            IGenericRepository<SubscriptionPlan> planRepository,
            ICurrentUserService currentUserService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            IGlobalLocalizer localizer,
            ILogger<SubscriptionService> logger)
        {
            _repository = repository;
            _planRepository = planRepository;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _mapper = mapper;
            _localizer = localizer;
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
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["SubscriptionNotFound"]);
                }

                var dto = _mapper.Map<SubscriptionDto>(subscription);

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
                return ApiResponse<SubscriptionDto>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["Subscription"]]
                );
            }
        }

        public async Task<ApiResponse<SubscriptionDto>> GetCurrentUserSubscriptionAsync()
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["UserNotAuthenticated"]);
                }

                var subscription = await _repository.AsQueryable(false)
                    .Include(s => s.Plan)
                    .Where(s => s.UserId == userId && s.IsActive)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["UserDoesNotHaveActiveSubscription"]
                    );
                }

                var dto = _mapper.Map<SubscriptionDto>(subscription);
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
                return ApiResponse<SubscriptionDto>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["Subscription"]]
                );
            }
        }

        public async Task<ApiResponse<PaginatedList<SubscriptionDto>>> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            string orderBy = "StartDate",
            string orderType = "DESC")
        {
            try
            {
                var query = _repository.AsQueryable(false);
                query = query.Include(s => s.Plan);

                if (!_currentUserService.User.IsInRole(AppRoles.Admin))
                {
                    var userId = _currentUserService.UserId;
                    query = query.Where(s => s.UserId == userId);
                }

                var paginatedResult = await _repository.GetPaginatedListAsync(
                    query, pageNumber, pageSize, orderBy, orderType);

                var dtos = _mapper.Map<List<SubscriptionDto>>(paginatedResult.Items);
                var userIds = dtos.Select(d => d.UserId).Distinct().ToList();
                var users = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName);

                foreach (var dto in dtos)
                {
                    if (users.TryGetValue(dto.UserId, out var name))
                    {
                        dto.UserName = name;
                    }
                }

                var result = new PaginatedList<SubscriptionDto>(
                    dtos,
                    paginatedResult.TotalCount,
                    paginatedResult.PageNumber,
                    pageSize);

                return ApiResponse<PaginatedList<SubscriptionDto>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated subscriptions");
                return ApiResponse<PaginatedList<SubscriptionDto>>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["Subscription"]]
                );
            }
        }

        public async Task<ApiResponse<SubscriptionDto>> CreateAsync(CreateSubscriptionDto dto)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["UserNotAuthenticated"]);
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (!await _userManager.IsInRoleAsync(user, AppRoles.User))
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["UserIsNotCustomer"]);
                }

                var plan = await _planRepository.GetByIdAsync(dto.PlanId);
                if (plan == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["SubscriptionPlanNotFound"]);
                }

                var existing = await _repository.AsQueryable(false)
                    .Where(s => s.UserId == userId && s.IsActive)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["UserAlreadyHasActiveSubscription"]);
                }

                var subscription = _mapper.Map<Subscription>(dto);
                subscription.UserId = userId;
                subscription.StartDate = DateTimeOffset.UtcNow;
                subscription.EndDate = subscription.StartDate.AddMonths(plan.DurationInMonths);

                _repository.Insert(subscription);
                await _repository.SaveChangesAsync();

                user.CurrentSubscriptionId = subscription.Id;
                await _userManager.UpdateAsync(user);

                subscription.Plan = plan;
                var entityDto = _mapper.Map<SubscriptionDto>(subscription);
                entityDto.UserName = user.FullName ?? user.UserName;

                return ApiResponse<SubscriptionDto>.Ok(
                    entityDto,
                    _localizer["SubscriptionCreatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return ApiResponse<SubscriptionDto>.Fail(
                    _localizer["ErrorOccurredWhileCreating", _localizer["Subscription"]]
                );
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
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["SubscriptionNotFound"]);
                }

                var userId = _currentUserService.UserId;
                if (!_currentUserService.User.IsInRole(AppRoles.Admin) && subscription.UserId != userId)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["UnauthorizedAccess"]
                    );
                }

                _mapper.Map(dto, subscription);
                _repository.Update(subscription);
                await _repository.SaveChangesAsync();

                var responseDto = _mapper.Map<SubscriptionDto>(subscription);
                var user = await _userManager.FindByIdAsync(subscription.UserId);
                if (user != null)
                {
                    responseDto.UserName = user.FullName ?? user.UserName;
                }

                return ApiResponse<SubscriptionDto>.Ok(
                    responseDto,
                    _localizer["SubscriptionUpdatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription with ID {SubscriptionId}", id);
                return ApiResponse<SubscriptionDto>.Fail(
                    _localizer["ErrorOccurredWhileUpdating", _localizer["Subscription"]]
                );
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var subscription = await _repository.GetByIdAsync(id);
                if (subscription == null)
                {
                    return ApiResponse<bool>.Fail(_localizer["SubscriptionNotFound"]);
                }

                var userId = _currentUserService.UserId;
                if (!_currentUserService.User.IsInRole(AppRoles.Admin) && subscription.UserId != userId)
                {
                    return ApiResponse<bool>.Fail(
                        _localizer["UnauthorizedAccess"]
                    );
                }

                _repository.Delete(subscription);
                await _repository.SaveChangesAsync();

                return ApiResponse<bool>.Ok(
                    true,
                    _localizer["SubscriptionDeletedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription with ID {SubscriptionId}", id);
                return ApiResponse<bool>.Fail(
                    _localizer["ErrorOccurredWhileDeleting", _localizer["Subscription"]]
                );
            }
        }

        public async Task<ApiResponse<bool>> CancelSubscriptionAsync(int id)
        {
            try
            {
                var subscription = await _repository.GetByIdAsync(id);
                if (subscription == null)
                {
                    return ApiResponse<bool>.Fail(_localizer["SubscriptionNotFound"]);
                }

                var userId = _currentUserService.UserId;
                if (!_currentUserService.User.IsInRole(AppRoles.Admin) && subscription.UserId != userId)
                {
                    return ApiResponse<bool>.Fail(
                        _localizer["UnauthorizedAccess"]
                    );
                }

                subscription.IsActive = false;
                _repository.Update(subscription);

                var user = await _userManager.FindByIdAsync(subscription.UserId);
                if (user != null && user.CurrentSubscriptionId == id)
                {
                    user.CurrentSubscriptionId = null;
                    await _userManager.UpdateAsync(user);
                }

                await _repository.SaveChangesAsync();

                return ApiResponse<bool>.Ok(
                    true,
                    _localizer["SubscriptionCanceledSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling subscription with ID {SubscriptionId}", id);
                return ApiResponse<bool>.Fail(
                    _localizer["ErrorOccurredWhileUpdating", _localizer["Subscription"]]
                );
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
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["SubscriptionNotFound"]);
                }

                var userId = _currentUserService.UserId;
                if (!_currentUserService.User.IsInRole(AppRoles.Admin) && subscription.UserId != userId)
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["UnauthorizedAccess"]);
                }

                if (!subscription.IsActive)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["CannotExtendInactiveSubscription"]
                    );
                }

                DateTimeOffset newEndDate;
                if (subscription.IsPaused)
                {
                    int daysRemaining = subscription.RemainingDays ?? 0;
                    newEndDate = DateTimeOffset.UtcNow.AddDays(daysRemaining)
                        .AddMonths(dto.AdditionalMonths);
                    subscription.RemainingDays = (int)(newEndDate - DateTimeOffset.UtcNow).TotalDays;
                }
                else
                {
                    newEndDate = subscription.EndDate.AddMonths(dto.AdditionalMonths);
                    subscription.EndDate = newEndDate;
                }

                _repository.Update(subscription);
                await _repository.SaveChangesAsync();

                var responseDto = _mapper.Map<SubscriptionDto>(subscription);
                var user = await _userManager.FindByIdAsync(subscription.UserId);
                if (user != null)
                {
                    responseDto.UserName = user.FullName ?? user.UserName;
                }

                return ApiResponse<SubscriptionDto>.Ok(
                    responseDto,
                    _localizer["SubscriptionExtendedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending subscription with ID {SubscriptionId}", dto.Id);
                return ApiResponse<SubscriptionDto>.Fail(
                    _localizer["ErrorOccurredWhileUpdating", _localizer["Subscription"]]
                );
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
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["SubscriptionNotFound"]);
                }

                var userId = _currentUserService.UserId;
                if (!_currentUserService.User.IsInRole(AppRoles.Admin) && subscription.UserId != userId)
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["UnauthorizedAccess"]);
                }

                if (!subscription.IsActive)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["CannotPauseInactiveSubscription"]
                    );
                }

                if (subscription.IsPaused)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["SubscriptionAlreadyPaused"]
                    );
                }

                var now = DateTimeOffset.UtcNow;
                if (now > subscription.EndDate)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["SubscriptionAlreadyExpired"]
                    );
                }

                subscription.IsPaused = true;
                subscription.PauseDate = now;
                subscription.RemainingDays = (int)(subscription.EndDate - now).TotalDays;

                _repository.Update(subscription);
                await _repository.SaveChangesAsync();

                var responseDto = _mapper.Map<SubscriptionDto>(subscription);
                var user = await _userManager.FindByIdAsync(subscription.UserId);
                if (user != null)
                {
                    responseDto.UserName = user.FullName ?? user.UserName;
                }

                return ApiResponse<SubscriptionDto>.Ok(
                    responseDto,
                    _localizer["SubscriptionPausedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing subscription with ID {SubscriptionId}", id);
                return ApiResponse<SubscriptionDto>.Fail(
                    _localizer["ErrorOccurredWhileUpdating", _localizer["Subscription"]]
                );
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
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["SubscriptionNotFound"]);
                }

                var userId = _currentUserService.UserId;
                if (!_currentUserService.User.IsInRole(AppRoles.Admin) && subscription.UserId != userId)
                {
                    return ApiResponse<SubscriptionDto>.Fail(_localizer["UnauthorizedAccess"]);
                }

                if (!subscription.IsActive)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["CannotResumeInactiveSubscription"]
                    );
                }

                if (!subscription.IsPaused)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["SubscriptionNotPaused"]
                    );
                }

                var remainingDays = subscription.RemainingDays ?? 0;
                if (remainingDays <= 0)
                {
                    return ApiResponse<SubscriptionDto>.Fail(
                        _localizer["SubscriptionHasNoRemainingDays"]
                    );
                }

                var now = DateTimeOffset.UtcNow;
                subscription.EndDate = now.AddDays(remainingDays);
                subscription.IsPaused = false;
                subscription.PauseDate = null;
                subscription.RemainingDays = null;

                _repository.Update(subscription);
                await _repository.SaveChangesAsync();

                var responseDto = _mapper.Map<SubscriptionDto>(subscription);
                var user = await _userManager.FindByIdAsync(subscription.UserId);
                if (user != null)
                {
                    responseDto.UserName = user.FullName;
                }

                return ApiResponse<SubscriptionDto>.Ok(
                    responseDto,
                    _localizer["SubscriptionResumedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming subscription with ID {SubscriptionId}", id);
                return ApiResponse<SubscriptionDto>.Fail(
                    _localizer["ErrorOccurredWhileUpdating", _localizer["Subscription"]]
                );
            }
        }
    }
}