using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.NonPeakSlots;
using PadelPass.Core.Common;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Services.Implementations
{
    public class NonPeakSlotService
    {
        private readonly IGenericRepository<NonPeakSlot> _repository;
        private readonly IGenericRepository<Club> _clubRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<NonPeakSlotService> _logger;
        private readonly IGlobalLocalizer _localizer;

        public NonPeakSlotService(
            IGenericRepository<NonPeakSlot> repository,
            IGenericRepository<Club> clubRepository,
            ICurrentUserService currentUserService,
            IMapper mapper,
            IGlobalLocalizer localizer,
            ILogger<NonPeakSlotService> logger)
        {
            _repository = repository;
            _clubRepository = clubRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<ApiResponse<NonPeakSlotDto>> GetByIdAsync(int id)
        {
            try
            {
                var slot = await _repository.AsQueryable(false)
                    .Include(s => s.Club)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (slot == null)
                    return ApiResponse<NonPeakSlotDto>.Fail(_localizer["NonPeakSlotNotFound"]);

                var dto = _mapper.Map<NonPeakSlotDto>(slot);
                return ApiResponse<NonPeakSlotDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting non-peak slot with ID {SlotId}", id);
                return ApiResponse<NonPeakSlotDto>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["NonPeakSlot"]]
                );
            }
        }

        public async Task<ApiResponse<PaginatedList<NonPeakSlotDto>>> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            int? clubId = null,
            string orderBy = "DayOfWeek",
            string orderType = "ASC")
        {
            try
            {
                var query = _repository.AsQueryable(false);

                if (clubId.HasValue)
                    query = query.Where(s => s.ClubId == clubId.Value);

                query = query.Include(s => s.Club);

                var paginatedResult = await _repository.GetPaginatedListAsync(
                    query, pageNumber, pageSize, orderBy, orderType);

                var mappedResult = new PaginatedList<NonPeakSlotDto>(
                    _mapper.Map<List<NonPeakSlotDto>>(paginatedResult.Items),
                    paginatedResult.TotalCount,
                    paginatedResult.PageNumber,
                    pageSize);

                return ApiResponse<PaginatedList<NonPeakSlotDto>>.Ok(mappedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated non-peak slots");
                return ApiResponse<PaginatedList<NonPeakSlotDto>>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["NonPeakSlot"]]
                );
            }
        }

        public async Task<ApiResponse<NonPeakSlotDto>> CreateAsync(CreateNonPeakSlotDto dto)
        {
            try
            {
                var club = await _clubRepository.GetByIdAsync(dto.ClubId);
                if (club == null)
                    return ApiResponse<NonPeakSlotDto>.Fail(_localizer["ClubNotFound"]);

                if (dto.EndTime <= dto.StartTime)
                    return ApiResponse<NonPeakSlotDto>.Fail(_localizer["EndTimeAfterStartTime"]);

                var slot = _mapper.Map<NonPeakSlot>(dto);
                _repository.Insert(slot);
                await _repository.SaveChangesAsync();

                slot.Club = club;
                var resultDto = _mapper.Map<NonPeakSlotDto>(slot);

                return ApiResponse<NonPeakSlotDto>.Ok(
                    resultDto,
                    _localizer["NonPeakSlotCreatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating non-peak slot");
                return ApiResponse<NonPeakSlotDto>.Fail(
                    _localizer["ErrorOccurredWhileCreating", _localizer["NonPeakSlot"]]
                );
            }
        }

        public async Task<ApiResponse<NonPeakSlotDto>> UpdateAsync(int id, UpdateNonPeakSlotDto dto)
        {
            try
            {
                var slot = await _repository.AsQueryable(true)
                    .Include(s => s.Club)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (slot == null)
                    return ApiResponse<NonPeakSlotDto>.Fail(_localizer["NonPeakSlotNotFound"]);

                if (dto.EndTime <= dto.StartTime)
                    return ApiResponse<NonPeakSlotDto>.Fail(_localizer["EndTimeAfterStartTime"]);

                _mapper.Map(dto, slot);
                _repository.Update(slot);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<NonPeakSlotDto>(slot);
                return ApiResponse<NonPeakSlotDto>.Ok(
                    resultDto,
                    _localizer["NonPeakSlotUpdatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating non-peak slot with ID {SlotId}", id);
                return ApiResponse<NonPeakSlotDto>.Fail(
                    _localizer["ErrorOccurredWhileUpdating", _localizer["NonPeakSlot"]]
                );
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var slot = await _repository.GetByIdAsync(id);
                if (slot == null)
                    return ApiResponse<bool>.Fail(_localizer["NonPeakSlotNotFound"]);

                _repository.Delete(slot);
                await _repository.SaveChangesAsync();

                return ApiResponse<bool>.Ok(
                    true,
                    _localizer["NonPeakSlotDeletedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting non-peak slot with ID {SlotId}", id);
                return ApiResponse<bool>.Fail(
                    _localizer["ErrorOccurredWhileDeleting", _localizer["NonPeakSlot"]]
                );
            }
        }
    }
}
