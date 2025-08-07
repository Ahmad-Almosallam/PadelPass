using AutoMapper;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.SubscriptionPlans;
using PadelPass.Application.Services;
using PadelPass.Core.Common;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Services.Implementations
{
    public class SubscriptionPlanService
    {
        private readonly IGenericRepository<SubscriptionPlan> _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<SubscriptionPlanService> _logger;
        private readonly IGlobalLocalizer _localizer;

        public SubscriptionPlanService(
            IGenericRepository<SubscriptionPlan> repository,
            ICurrentUserService currentUserService,
            IMapper mapper,
            IGlobalLocalizer localizer,
            ILogger<SubscriptionPlanService> logger)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<ApiResponse<SubscriptionPlanDto>> GetByIdAsync(int id)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(id);
                if (plan == null)
                {
                    return ApiResponse<SubscriptionPlanDto>
                        .Fail(_localizer["SubscriptionPlanNotFound"]);
                }

                var dto = _mapper.Map<SubscriptionPlanDto>(plan);
                return ApiResponse<SubscriptionPlanDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription plan with ID {PlanId}", id);
                return ApiResponse<SubscriptionPlanDto>
                    .Fail(_localizer["ErrorOccurredWhileRetrieving", _localizer["SubscriptionPlan"]]);
            }
        }

        public async Task<ApiResponse<PaginatedList<SubscriptionPlanDto>>> GetPaginatedAsync(
            int pageNumber, int pageSize, string orderBy = "Name", string orderType = "ASC")
        {
            try
            {
                var query = _repository.AsQueryable(false);
                var paginatedResult = await _repository.GetPaginatedListAsync(
                    query, pageNumber, pageSize, orderBy, orderType);

                var mappedResult = new PaginatedList<SubscriptionPlanDto>(
                    _mapper.Map<List<SubscriptionPlanDto>>(paginatedResult.Items),
                    paginatedResult.TotalCount,
                    paginatedResult.PageNumber,
                    pageSize);

                return ApiResponse<PaginatedList<SubscriptionPlanDto>>.Ok(mappedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated subscription plans");
                return ApiResponse<PaginatedList<SubscriptionPlanDto>>
                    .Fail(_localizer["ErrorOccurredWhileRetrieving", _localizer["SubscriptionPlan"]]);
            }
        }

        public async Task<ApiResponse<SubscriptionPlanDto>> CreateAsync(CreateSubscriptionPlanDto dto)
        {
            try
            {
                var plan = _mapper.Map<SubscriptionPlan>(dto);
                _repository.Insert(plan);
                await _repository.SaveChangesAsync();

                return ApiResponse<SubscriptionPlanDto>.Ok(
                    _mapper.Map<SubscriptionPlanDto>(plan),
                    _localizer["SubscriptionPlanCreatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription plan");
                return ApiResponse<SubscriptionPlanDto>
                    .Fail(_localizer["ErrorOccurredWhileCreating", _localizer["SubscriptionPlan"]]);
            }
        }

        public async Task<ApiResponse<SubscriptionPlanDto>> UpdateAsync(int id, UpdateSubscriptionPlanDto dto)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(id);
                if (plan == null)
                {
                    return ApiResponse<SubscriptionPlanDto>
                        .Fail(_localizer["SubscriptionPlanNotFound"]);
                }

                _mapper.Map(dto, plan);
                _repository.Update(plan);
                await _repository.SaveChangesAsync();

                return ApiResponse<SubscriptionPlanDto>.Ok(
                    _mapper.Map<SubscriptionPlanDto>(plan),
                    _localizer["SubscriptionPlanUpdatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription plan with ID {PlanId}", id);
                return ApiResponse<SubscriptionPlanDto>
                    .Fail(_localizer["ErrorOccurredWhileUpdating", _localizer["SubscriptionPlan"]]);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(id);
                if (plan == null)
                {
                    return ApiResponse<bool>.Fail(_localizer["SubscriptionPlanNotFound"]);
                }

                _repository.Delete(plan);
                await _repository.SaveChangesAsync();

                return ApiResponse<bool>.Ok(
                    true,
                    _localizer["SubscriptionPlanDeletedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription plan with ID {PlanId}", id);
                return ApiResponse<bool>
                    .Fail(_localizer["ErrorOccurredWhileDeleting", _localizer["SubscriptionPlan"]]);
            }
        }
    }
}
