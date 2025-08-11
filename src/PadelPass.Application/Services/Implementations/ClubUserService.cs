using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.ClubUsers;
using PadelPass.Core.Common;
using PadelPass.Core.Common.Enums;
using PadelPass.Core.Constants;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Services.Implementations
{
    public class ClubUserService
    {
        private readonly IGenericRepository<ClubUser> _repository;
        private readonly IGenericRepository<Club> _clubRepository;
        private readonly IGenericRepository<Subscription> _subscriptionRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<ClubUserService> _logger;
        private readonly IGlobalLocalizer _localizer;

        public ClubUserService(
            IGenericRepository<ClubUser> repository,
            IGenericRepository<Club> clubRepository,
            IGenericRepository<Subscription> subscriptionRepository,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IMapper mapper,
            IGlobalLocalizer localizer,
            ILogger<ClubUserService> logger)
        {
            _repository = repository;
            _clubRepository = clubRepository;
            _subscriptionRepository = subscriptionRepository;
            _userManager = userManager;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<ApiResponse<ClubUserDto>> GetByIdAsync(int id)
        {
            try
            {
                var clubUser = await _repository.AsQueryable(false)
                    .Include(cu => cu.Club)
                    .Include(cu => cu.User)
                    .FirstOrDefaultAsync(cu => cu.Id == id);

                if (clubUser == null)
                {
                    return ApiResponse<ClubUserDto>.Fail(_localizer["ClubUserNotFound"]);
                }

                var dto = _mapper.Map<ClubUserDto>(clubUser);
                return ApiResponse<ClubUserDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ClubUser with ID {ClubUserId}", id);
                return ApiResponse<ClubUserDto>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["ClubUser"]]
                );
            }
        }

        public async Task<ApiResponse<PaginatedList<ClubUserDto>>> GetPaginatedAsync(
            int pageNumber, int pageSize, int? clubId = null, string orderBy = "Id", string orderType = "ASC")
        {
            try
            {
                var query = _repository.AsQueryable(false);
                query = query.Include(cu => cu.Club)
                    .Include(cu => cu.User);

                if (clubId.HasValue)
                {
                    query = query.Where(cu => cu.ClubId == clubId.Value);
                }

                if (_currentUserService.User.IsInRole(AppRoles.ClubUser) &&
                    !_currentUserService.User.IsInRole(AppRoles.Admin))
                {
                    var userId = _currentUserService.UserId;
                    var userClubs = await _repository.AsQueryable(false)
                        .Where(cu => cu.UserId == userId && cu.IsActive)
                        .Select(cu => cu.ClubId)
                        .ToListAsync();

                    if (userClubs.Any())
                    {
                        query = query.Where(cu => userClubs.Contains(cu.ClubId));
                    }
                    else
                    {
                        return ApiResponse<PaginatedList<ClubUserDto>>.Ok(
                            new PaginatedList<ClubUserDto>(new List<ClubUserDto>(), 0, pageNumber, pageSize)
                        );
                    }
                }

                var paginatedResult = await _repository.GetPaginatedListAsync(
                    query, pageNumber, pageSize, orderBy, orderType
                );

                var mappedResult = new PaginatedList<ClubUserDto>(
                    _mapper.Map<List<ClubUserDto>>(paginatedResult.Items),
                    paginatedResult.TotalCount,
                    paginatedResult.PageNumber,
                    pageSize
                );

                return ApiResponse<PaginatedList<ClubUserDto>>.Ok(mappedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated ClubUsers");
                return ApiResponse<PaginatedList<ClubUserDto>>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["ClubUser"]]
                );
            }
        }

        public async Task<ApiResponse<ClubUserDto>> CreateAsync(CreateClubUserDto dto)
        {
            try
            {
                var club = await _clubRepository.GetByIdAsync(dto.ClubId);
                if (club == null)
                    return ApiResponse<ClubUserDto>.Fail(_localizer["ClubNotFound"]);

                string userId = dto.UserId;
                ApplicationUser user;

                if (string.IsNullOrEmpty(userId) && dto.RegisterDto != null)
                {
                    var registerDto = dto.RegisterDto;
                    if (registerDto.Password != registerDto.ConfirmPassword)
                        return ApiResponse<ClubUserDto>.Fail(_localizer["PasswordConfirmationDoNotMatch"]);

                    var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                    if (existingUser != null)
                        return ApiResponse<ClubUserDto>.Fail(_localizer["UserWithEmailExists"]);
                    
                    
                    var userExists = await _userManager.Users.AnyAsync(x => x.PhoneNumber == registerDto.PhoneNumber);
                    if (userExists)
                    {
                        return ApiResponse<ClubUserDto>.Fail(
                            _localizer["UserWithPhoneNumberExists", registerDto.PhoneNumber]
                        );
                    }

                    user = new ApplicationUser
                    {
                        UserName = registerDto.Email,
                        Email = registerDto.Email,
                        FullName = registerDto.FullName,
                        PhoneNumber = registerDto.PhoneNumber,
                        UserType = UserType.BranchUser,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(user, registerDto.Password);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        return ApiResponse<ClubUserDto>.Fail(
                            _localizer["FailedToCreateUser", errors]
                        );
                    }

                    await _userManager.AddToRoleAsync(user, AppRoles.User);
                    userId = user.Id;
                }
                else if (string.IsNullOrEmpty(userId))
                {
                    return ApiResponse<ClubUserDto>.Fail(
                        _localizer["EitherUserIdOrRegisterDtoRequired"]
                    );
                }
                else
                {
                    user = await _userManager.FindByIdAsync(userId);
                    if (user == null)
                        return ApiResponse<ClubUserDto>.Fail(_localizer["UserNotFound"]);
                }

                if (!await _userManager.IsInRoleAsync(user, AppRoles.ClubUser))
                    await _userManager.AddToRoleAsync(user, AppRoles.ClubUser);

                var existingClubUser = await _repository.AsQueryable(false)
                    .FirstOrDefaultAsync(cu => cu.ClubId == dto.ClubId && cu.UserId == userId);

                if (existingClubUser != null)
                    return ApiResponse<ClubUserDto>.Fail(
                        _localizer["UserAlreadyAssociatedWithClub"]
                    );

                var clubUser = new ClubUser { ClubId = dto.ClubId, UserId = userId, IsActive = true };
                _repository.Insert(clubUser);
                await _repository.SaveChangesAsync();

                clubUser.Club = club;
                clubUser.User = user;

                var resultDto = _mapper.Map<ClubUserDto>(clubUser);
                return ApiResponse<ClubUserDto>.Ok(
                    resultDto,
                    _localizer["ClubUserCreatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ClubUser");
                return ApiResponse<ClubUserDto>.Fail(
                    _localizer["ErrorOccurredWhileCreating", _localizer["ClubUser"]]
                );
            }
        }

        public async Task<ApiResponse<ClubUserDto>> UpdateAsync(int id, UpdateClubUserDto dto)
        {
            try
            {
                var clubUser = await _repository.AsQueryable(true)
                    .Include(cu => cu.Club)
                    .Include(cu => cu.User)
                    .FirstOrDefaultAsync(cu => cu.Id == id);

                if (clubUser == null)
                    return ApiResponse<ClubUserDto>.Fail(_localizer["ClubUserNotFound"]);

                clubUser.IsActive = dto.IsActive;
                clubUser.User.IsActive = dto.IsActive;
                _repository.Update(clubUser);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<ClubUserDto>(clubUser);
                return ApiResponse<ClubUserDto>.Ok(
                    resultDto,
                    _localizer["ClubUserUpdatedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ClubUser with ID {ClubUserId}", id);
                return ApiResponse<ClubUserDto>.Fail(
                    _localizer["ErrorOccurredWhileUpdating", _localizer["ClubUser"]]
                );
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var clubUser = await _repository.GetByIdAsync(id);
                if (clubUser == null)
                    return ApiResponse<bool>.Fail(_localizer["ClubUserNotFound"]);
                
                
                var user = await _userManager.FindByIdAsync(clubUser.UserId);
                if (user == null)
                {
                    return ApiResponse<bool>.Fail(_localizer["UserNotFound"]);
                }
                
                if (await _userManager.IsInRoleAsync(user, AppRoles.ClubUser))
                {
                    var res = await _userManager.DeleteAsync(user);
                    if (!res.Succeeded)
                    {
                        var errors = string.Join(", ", res.Errors.Select(e => e.Description));
                        return ApiResponse<bool>.Fail(
                            _localizer["FailedToDeleteUser", errors]
                        );
                    }
                }

                return ApiResponse<bool>.Ok(
                    true,
                    _localizer["ClubUserDeletedSuccessfully"]
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ClubUser with ID {ClubUserId}", id);
                return ApiResponse<bool>.Fail(
                    _localizer["ErrorOccurredWhileDeleting", _localizer["ClubUser"]]
                );
            }
        }

        public async Task<ApiResponse<PaginatedList<UserSearchDto>>> SearchUsersAsync(UserSearchQueryDto query)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var isAdmin = _currentUserService.User.IsInRole(AppRoles.Admin) ||
                              _currentUserService.User.IsInRole(AppRoles.SuperAdmin);
                var isClubUser = _currentUserService.User.IsInRole(AppRoles.ClubUser);

                if (!isAdmin && !isClubUser)
                    return ApiResponse<PaginatedList<UserSearchDto>>.Fail(_localizer["UnauthorizedAccess"]);

                List<int> accessibleClubIds = new List<int>();
                if (!isAdmin && isClubUser)
                {
                    accessibleClubIds = await _repository.AsQueryable(false)
                        .Where(cu => cu.UserId == currentUserId && cu.IsActive)
                        .Select(cu => cu.ClubId)
                        .ToListAsync();

                    if (query.ClubId.HasValue && !accessibleClubIds.Contains(query.ClubId.Value))
                        return ApiResponse<PaginatedList<UserSearchDto>>.Fail(_localizer["NoAccessToThisClub"]);

                    if (!accessibleClubIds.Any())
                        return ApiResponse<PaginatedList<UserSearchDto>>.Fail(_localizer["NoAccessToClubs"]);
                }

                var usersInUserRole = await _userManager.GetUsersInRoleAsync(AppRoles.User);
                var userIds = usersInUserRole.Select(u => u.Id).ToList();

                var userQuery = _userManager.Users.Where(u => userIds.Contains(u.Id));
                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    userQuery = userQuery.Where(u =>
                        u.Email.Contains(query.SearchTerm) ||
                        u.UserName.Contains(query.SearchTerm) ||
                        u.FullName.Contains(query.SearchTerm) ||
                        u.PhoneNumber.Contains(query.SearchTerm));
                }

                var subscriptions = await _subscriptionRepository.AsQueryable(false)
                    .Include(s => s.Plan)
                    .Where(s => s.IsActive && !s.IsPaused && s.EndDate > DateTimeOffset.UtcNow)
                    .ToListAsync();

                var subscriptionsByUserId = subscriptions
                    .GroupBy(s => s.UserId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.EndDate).First());

                List<string> filteredUserIds = userIds;
                if (query.HasActiveSubscription.HasValue)
                {
                    filteredUserIds = query.HasActiveSubscription.Value
                        ? subscriptionsByUserId.Keys.ToList()
                        : userIds.Where(id => !subscriptionsByUserId.ContainsKey(id)).ToList();

                    userQuery = userQuery.Where(u => filteredUserIds.Contains(u.Id));
                }

                var totalCount = await userQuery.CountAsync();
                var pagedUsers = await userQuery
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var userDtos = pagedUsers.Select(user =>
                {
                    var hasActive = subscriptionsByUserId.ContainsKey(user.Id);
                    var sub = hasActive ? subscriptionsByUserId[user.Id] : null;
                    return new UserSearchDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        HasActiveSubscription = hasActive,
                        SubscriptionPlanName = hasActive ? sub.Plan.Name : null,
                        SubscriptionEndDate = hasActive ? sub.EndDate : null
                    };
                }).ToList();

                var result = new PaginatedList<UserSearchDto>(
                    userDtos, totalCount, query.PageNumber, query.PageSize
                );

                return ApiResponse<PaginatedList<UserSearchDto>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return ApiResponse<PaginatedList<UserSearchDto>>.Fail(_localizer["UnexpectedErrorOccurred"]);
            }
        }

        public async Task<ApiResponse<UserSearchDto>> GetUserWithSubscriptionDetailsAsync(string phoneNumber)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                var isAdmin = _currentUserService.User.IsInRole(AppRoles.Admin) ||
                              _currentUserService.User.IsInRole(AppRoles.SuperAdmin);
                var isClubUser = _currentUserService.User.IsInRole(AppRoles.ClubUser);

                if (!isAdmin && !isClubUser)
                    return ApiResponse<UserSearchDto>.Fail(_localizer["UnauthorizedAccess"]);

                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
                if (user == null)
                    return ApiResponse<UserSearchDto>.Fail(_localizer["UserNotFound"]);

                if (!await _userManager.IsInRoleAsync(user, AppRoles.User))
                    return ApiResponse<UserSearchDto>.Fail(
                        _localizer["UserIsNotRegularUser", phoneNumber]
                    );

                var subscription = await _subscriptionRepository.AsQueryable(false)
                    .Include(s => s.Plan)
                    .Where(s => s.UserId == user.Id && s.IsActive && !s.IsPaused && s.EndDate > DateTimeOffset.UtcNow)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();

                var userDto = new UserSearchDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    HasActiveSubscription = subscription != null,
                    SubscriptionPlanName = subscription?.Plan.Name,
                    SubscriptionEndDate = subscription?.EndDate
                };

                return ApiResponse<UserSearchDto>.Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with subscription details for user ID {UserId}", phoneNumber);
                return ApiResponse<UserSearchDto>.Fail(
                    _localizer["ErrorOccurredWhileRetrieving", _localizer["User"]]
                );
            }
        }

        public async Task<ApiResponse<bool>> CheckUserSubscriptionByPhoneAsync(string phoneNumber)
        {
            try
            {
                var isAdmin = _currentUserService.User.IsInRole(AppRoles.Admin) ||
                              _currentUserService.User.IsInRole(AppRoles.SuperAdmin);
                var isClubUser = _currentUserService.User.IsInRole(AppRoles.ClubUser);

                if (!isAdmin && !isClubUser)
                    return ApiResponse<bool>.Fail(_localizer["UnauthorizedAccess"]);

                if (!isAdmin && isClubUser)
                {
                    var userId = _currentUserService.UserId;
                    var hasClubAccess = await _repository.AsQueryable(false)
                        .AnyAsync(cu => cu.UserId == userId && cu.IsActive);

                    if (!hasClubAccess)
                        return ApiResponse<bool>.Fail(_localizer["NoAccessToClubs"]);
                }

                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                if (user == null)
                    return ApiResponse<bool>.Ok(
                        false,
                        _localizer["NoUserFoundWithPhoneNumber", phoneNumber]
                    );

                if (!await _userManager.IsInRoleAsync(user, AppRoles.User))
                    return ApiResponse<bool>.Ok(
                        false,
                        _localizer["PhoneNumberBelongsToNonRegularUser"]
                    );

                var hasSub = await _subscriptionRepository.AsQueryable(false)
                    .AnyAsync(s =>
                        s.UserId == user.Id && s.IsActive && !s.IsPaused && s.EndDate > DateTimeOffset.UtcNow);

                return ApiResponse<bool>.Ok(hasSub);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription for user with phone number {PhoneNumber}",
                    phoneNumber);
                return ApiResponse<bool>.Fail(_localizer["UnexpectedErrorOccurred"]);
            }
        }
    }
}