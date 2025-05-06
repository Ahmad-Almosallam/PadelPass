using AutoMapper;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.Clubs;
using PadelPass.Application.Services;
using PadelPass.Core.Common;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Services.Implementations;

public class ClubService
{
    private readonly IGenericRepository<Club> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<ClubService> _logger;

    public ClubService(
        IGenericRepository<Club> repository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<ClubService> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<ClubDto>> GetByIdAsync(int id)
    {
        try
        {
            var club = await _repository.GetByIdAsync(id);
            if (club == null)
            {
                return ApiResponse<ClubDto>.Fail($"Club with ID {id} not found");
            }

            return ApiResponse<ClubDto>.Ok(_mapper.Map<ClubDto>(club));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting club with ID {ClubId}", id);
            return ApiResponse<ClubDto>.Fail("An error occurred while retrieving the club");
        }
    }

    public async Task<ApiResponse<PaginatedList<ClubDto>>> GetPaginatedAsync(
        int pageNumber, int pageSize, string orderBy = "Name", string orderType = "ASC")
    {
        try
        {
            var query = _repository.AsQueryable(false);
            
            var paginatedResult = await _repository.GetPaginatedListAsync(
                query, pageNumber, pageSize, orderBy, orderType);
            
            var mappedResult = new PaginatedList<ClubDto>(
                _mapper.Map<List<ClubDto>>(paginatedResult.Items),
                paginatedResult.TotalCount,
                paginatedResult.PageNumber,
                pageSize);
            
            return ApiResponse<PaginatedList<ClubDto>>.Ok(mappedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated clubs");
            return ApiResponse<PaginatedList<ClubDto>>.Fail("An error occurred while retrieving clubs");
        }
    }

    public async Task<ApiResponse<ClubDto>> CreateAsync(CreateClubDto dto)
    {
        try
        {
            var club = _mapper.Map<Club>(dto);
            
            _repository.Insert(club);
            await _repository.SaveChangesAsync();
            
            return ApiResponse<ClubDto>.Ok(
                _mapper.Map<ClubDto>(club), 
                "Club created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating club");
            return ApiResponse<ClubDto>.Fail("An error occurred while creating the club");
        }
    }

    public async Task<ApiResponse<ClubDto>> UpdateAsync(int id, UpdateClubDto dto)
    {
        try
        {
            var club = await _repository.GetByIdAsync(id);
            if (club == null)
            {
                return ApiResponse<ClubDto>.Fail($"Club with ID {id} not found");
            }

            _mapper.Map(dto, club);
            
            _repository.Update(club);
            await _repository.SaveChangesAsync();
            
            return ApiResponse<ClubDto>.Ok(
                _mapper.Map<ClubDto>(club), 
                "Club updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating club with ID {ClubId}", id);
            return ApiResponse<ClubDto>.Fail("An error occurred while updating the club");
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var club = await _repository.GetByIdAsync(id);
            if (club == null)
            {
                return ApiResponse<bool>.Fail($"Club with ID {id} not found");
            }

            _repository.Delete(club);
            await _repository.SaveChangesAsync();
            
            return ApiResponse<bool>.Ok(true, "Club deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting club with ID {ClubId}", id);
            return ApiResponse<bool>.Fail("An error occurred while deleting the club");
        }
    }
}
