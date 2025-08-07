using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.CheckIns;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;

namespace PadelPass.Application.Services.Implementations;

public class CheckInService
{
    private readonly IGenericRepository<CheckIn> _checkInRepository;
    private readonly IGenericRepository<Club> _clubRepository;
    private readonly IGenericRepository<Subscription> _subscriptionRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly IMapper _mapper;
    private readonly ILogger<CheckInService> _logger;
    private readonly IGlobalLocalizer _localizer;

    public CheckInService(
        IGenericRepository<CheckIn> checkInRepository,
        IGenericRepository<Club> clubRepository,
        IGenericRepository<Subscription> subscriptionRepository,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService,
        ITimeZoneService timeZoneService,
        IMapper mapper,
        ILogger<CheckInService> logger,
        IGlobalLocalizer localizer)
    {
        _checkInRepository = checkInRepository;
        _clubRepository = clubRepository;
        _subscriptionRepository = subscriptionRepository;
        _userManager = userManager;
        _currentUserService = currentUserService;
        _timeZoneService = timeZoneService;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<ApiResponse<CheckInDto>> CreateCheckInAsync(CreateCheckInDto dto)
    {
        try
        {
            // Find user by phone number
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == dto.UserPhoneNumber);

            if (user == null)
            {
                return ApiResponse<CheckInDto>.Fail(_localizer["NoUserFoundWithPhoneNumber", dto.UserPhoneNumber]);
            }

            // Verify user has User role
            if (!await _userManager.IsInRoleAsync(user, AppRoles.User))
            {
                return ApiResponse<CheckInDto>.Fail(_localizer["InvalidUserType"]);
            }

            // Get club with timezone info
            var club = await _clubRepository.GetByIdAsync(dto.ClubId);
            if (club == null)
            {
                return ApiResponse<CheckInDto>.Fail(_localizer["ClubNotFound"]);
            }

            // Convert check-in time to UTC if provided, or use current time
            var checkInTimeUtc = dto.CheckInDateTime.HasValue
                ? _timeZoneService.ConvertToUtc(dto.CheckInDateTime.Value, club.TimeZoneId)
                : DateTimeOffset.UtcNow;

            // Get club's current date for duplicate check validation
            var clubTime = _timeZoneService.ConvertToClubTime(checkInTimeUtc, club.TimeZoneId);
            var clubDate = clubTime.Date;

            // Check active subscription
            var hasActiveSubscription = await _subscriptionRepository.AsQueryable(false)
                .AnyAsync(s => s.UserId == user.Id &&
                               s.IsActive &&
                               !s.IsPaused &&
                               s.EndDate > DateTimeOffset.UtcNow);

            if (!hasActiveSubscription)
            {
                return ApiResponse<CheckInDto>.Fail(_localizer["UserDoesNotHaveActiveSubscription"]);
            }

            // Check if already checked in today (based on club's timezone)
            var alreadyCheckedInToday = await _checkInRepository.AsQueryable(false)
                .Include(c => c.Club)
                .AnyAsync(c => c.UserId == user.Id &&
                               c.ClubId == dto.ClubId);

            if (alreadyCheckedInToday)
            {
                // Additional check: verify it's the same day in club's timezone
                var existingCheckIn = await _checkInRepository.AsQueryable(false)
                    .Where(c => c.UserId == user.Id && c.ClubId == dto.ClubId)
                    .OrderByDescending(c => c.CheckInDateTime)
                    .FirstOrDefaultAsync();

                if (existingCheckIn != null)
                {
                    var existingClubTime =
                        _timeZoneService.ConvertToClubTime(existingCheckIn.CheckInDateTime, club.TimeZoneId);
                    if (existingClubTime.Date == clubDate)
                    {
                        return ApiResponse<CheckInDto>.Fail(_localizer["UserAlreadyCheckedInToday"]);
                    }
                }
            }

            // Validate non-peak hours if required
            var nonPeakSlots = await _clubRepository.AsQueryable(false)
                .Where(c => c.Id == dto.ClubId)
                .SelectMany(c => c.NonPeakSlots)
                .ToListAsync();

            if (nonPeakSlots.Any())
            {
                var isWithinNonPeak = _timeZoneService.IsWithinNonPeakHours(clubTime, nonPeakSlots);
                if (!isWithinNonPeak)
                {
                    return ApiResponse<CheckInDto>.Fail(_localizer["CheckInOnlyDuringNonPeakHours"]);
                }
            }

            // Create check-in (store in UTC)
            var checkIn = new CheckIn
            {
                UserId = user.Id,
                ClubId = dto.ClubId,
                CheckInDateTime = checkInTimeUtc,
                CourtNumber = dto.CourtNumber,
                StartPlayTime = dto.StartPlayTime?.ToUniversalTime(),
                PlayDurationMinutes = dto.PlayDurationMinutes,
                Notes = dto.Notes,
                CheckedInBy = _currentUserService.UserId
            };

            _checkInRepository.Insert(checkIn);
            await _checkInRepository.SaveChangesAsync();

            // Map to DTO (convert back to club time for display)
            var resultDto = _mapper.Map<CheckInDto>(checkIn);
            resultDto.UserName = user.FullName;
            resultDto.UserPhoneNumber = user.PhoneNumber;
            resultDto.ClubName = club.Name;
            resultDto.CheckInDateTime = _timeZoneService.ConvertToClubTime(checkIn.CheckInDateTime, club.TimeZoneId);

            return ApiResponse<CheckInDto>.Ok(resultDto, _localizer["CheckInSuccessful", user.FullName]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating check-in for user {PhoneNumber}", dto.UserPhoneNumber);
            return ApiResponse<CheckInDto>.Fail(_localizer["ErrorOccurredWhileProcessingCheckIn"]);
        }
    }
}