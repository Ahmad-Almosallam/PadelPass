using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PadelPass.Application.DTOs.ClubUsers;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Services.Implementations;

public class ClubUserService
{
    private readonly IGenericRepository<ClubUser> _repository;
    private readonly IGenericRepository<Club> _clubRepository;
    private readonly IGenericRepository<Subscription> _subscriptionRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<ClubUserService> _logger;

    public ClubUserService(
        IGenericRepository<ClubUser> repository,
        IGenericRepository<Club> clubRepository,
        IGenericRepository<Subscription> subscriptionRepository,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<ClubUserService> logger)
    {
        _repository = repository;
        _clubRepository = clubRepository;
        _subscriptionRepository = subscriptionRepository;
        _userManager = userManager;
        _currentUserService = currentUserService;
        _mapper = mapper;
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
                return ApiResponse<ClubUserDto>.Fail($"ClubUser with ID {id} not found");
            }

            var dto = _mapper.Map<ClubUserDto>(clubUser);
            return ApiResponse<ClubUserDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ClubUser with ID {ClubUserId}", id);
            return ApiResponse<ClubUserDto>.Fail("An error occurred while retrieving the ClubUser");
        }
    }

    public async Task<ApiResponse<PaginatedList<ClubUserDto>>> GetPaginatedAsync(
        int pageNumber, int pageSize, int? clubId = null, string orderBy = "Id", string orderType = "ASC")
    {
        try
        {
            var query = _repository.AsQueryable(false);
            query = query
                .Include(cu => cu.Club)
                .Include(cu => cu.User);

            if (clubId.HasValue)
            {
                query = query.Where(cu => cu.ClubId == clubId.Value);
            }

            // If current user is a ClubUser, only show their club's users
            if (_currentUserService.User.IsInRole(AppRoles.ClubUser) && !_currentUserService.User.IsInRole(AppRoles.Admin))
            {
                // Find the ClubUser entry for the current user
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
                    // If the user doesn't have any clubs, return an empty list
                    return ApiResponse<PaginatedList<ClubUserDto>>.Ok(
                        new PaginatedList<ClubUserDto>(new List<ClubUserDto>(), 0, pageNumber, pageSize));
                }
            }

            var paginatedResult = await _repository.GetPaginatedListAsync(
                query, pageNumber, pageSize, orderBy, orderType);

            var mappedResult = new PaginatedList<ClubUserDto>(
                _mapper.Map<List<ClubUserDto>>(paginatedResult.Items),
                paginatedResult.TotalCount,
                paginatedResult.PageNumber,
                pageSize);

            return ApiResponse<PaginatedList<ClubUserDto>>.Ok(mappedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated ClubUsers");
            return ApiResponse<PaginatedList<ClubUserDto>>.Fail("An error occurred while retrieving ClubUsers");
        }
    }

    public async Task<ApiResponse<ClubUserDto>> CreateAsync(CreateClubUserDto dto)
    {
        try
        {
            // Validate club exists
            var club = await _clubRepository.GetByIdAsync(dto.ClubId);
            if (club == null)
            {
                return ApiResponse<ClubUserDto>.Fail($"Club with ID {dto.ClubId} not found");
            }

            string userId = dto.UserId;
            ApplicationUser user;
    
            // If userId is not provided but RegisterDto is, create a new user
            if (string.IsNullOrEmpty(userId) && dto.RegisterDto != null)
            {
                var registerDto = dto.RegisterDto;
                
                // Validate the password confirmation
                if (registerDto.Password != registerDto.ConfirmPassword)
                {
                    return ApiResponse<ClubUserDto>.Fail("Password and confirmation do not match");
                }
                
                // Check if user with this email already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return ApiResponse<ClubUserDto>.Fail($"User with email {registerDto.Email} already exists");
                }
                
                // Create the new user
                user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FullName = registerDto.FullName,
                    PhoneNumber = registerDto.PhoneNumber,
                    EmailConfirmed = true // Set to true for simplicity, may need email verification in production
                };
                
                var createResult = await _userManager.CreateAsync(user, registerDto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return ApiResponse<ClubUserDto>.Fail($"Failed to create user: {errors}");
                }
                
                // Assign default user role
                await _userManager.AddToRoleAsync(user, AppRoles.User);
                
                userId = user.Id;
            }
            else if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<ClubUserDto>.Fail("Either UserId or RegisterDto must be provided");
            }
            else
            {
                // Validate existing user
                user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<ClubUserDto>.Fail($"User with ID {userId} not found");
                }
            }
    
            // Ensure user has ClubUser role
            if (!await _userManager.IsInRoleAsync(user, AppRoles.ClubUser))
            {
                await _userManager.AddToRoleAsync(user, AppRoles.ClubUser);
            }
    
            // Check if ClubUser relationship already exists
            var existingClubUser = await _repository.AsQueryable(false)
                .FirstOrDefaultAsync(cu => cu.ClubId == dto.ClubId && cu.UserId == userId);
    
            if (existingClubUser != null)
            {
                return ApiResponse<ClubUserDto>.Fail("This user is already associated with this club");
            }
    
            // Create new ClubUser
            var clubUser = new ClubUser
            {
                ClubId = dto.ClubId,
                UserId = userId,
                IsActive = true
            };

            _repository.Insert(clubUser);
            await _repository.SaveChangesAsync();

            // Load relationships for mapping
            clubUser.Club = club;
            clubUser.User = user;

            var resultDto = _mapper.Map<ClubUserDto>(clubUser);
            return ApiResponse<ClubUserDto>.Ok(resultDto, "Club user created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ClubUser");
            return ApiResponse<ClubUserDto>.Fail("An error occurred while creating the club user");
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
            {
                return ApiResponse<ClubUserDto>.Fail($"ClubUser with ID {id} not found");
            }

            // Update properties
            clubUser.IsActive = dto.IsActive;

            _repository.Update(clubUser);
            await _repository.SaveChangesAsync();

            var resultDto = _mapper.Map<ClubUserDto>(clubUser);
            return ApiResponse<ClubUserDto>.Ok(resultDto, "Club user updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ClubUser with ID {ClubUserId}", id);
            return ApiResponse<ClubUserDto>.Fail("An error occurred while updating the club user");
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var clubUser = await _repository.GetByIdAsync(id);
            if (clubUser == null)
            {
                return ApiResponse<bool>.Fail($"ClubUser with ID {id} not found");
            }

            _repository.Delete(clubUser);
            await _repository.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Club user deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ClubUser with ID {ClubUserId}", id);
            return ApiResponse<bool>.Fail("An error occurred while deleting the club user");
        }
    }

    public async Task<ApiResponse<PaginatedList<UserSearchDto>>> SearchUsersAsync(UserSearchQueryDto query)
    {
        try
        {
            // Get club access for current user if they are a ClubUser
            var currentUserId = _currentUserService.UserId;
            var isAdmin = _currentUserService.User.IsInRole(AppRoles.Admin) || 
                          _currentUserService.User.IsInRole(AppRoles.SuperAdmin);
            var isClubUser = _currentUserService.User.IsInRole(AppRoles.ClubUser);
            
            if (!isAdmin && !isClubUser)
            {
                return ApiResponse<PaginatedList<UserSearchDto>>.Fail("Unauthorized access");
            }

            List<int> accessibleClubIds = new List<int>();
            
            if (!isAdmin && isClubUser)
            {
                // Get clubs the ClubUser has access to
                accessibleClubIds = await _repository.AsQueryable(false)
                    .Where(cu => cu.UserId == currentUserId && cu.IsActive)
                    .Select(cu => cu.ClubId)
                    .ToListAsync();
                
                if (query.ClubId.HasValue && !accessibleClubIds.Contains(query.ClubId.Value))
                {
                    return ApiResponse<PaginatedList<UserSearchDto>>.Fail("You don't have access to this club");
                }
                
                if (!accessibleClubIds.Any())
                {
                    return ApiResponse<PaginatedList<UserSearchDto>>.Fail("You don't have access to any clubs");
                }
            }

            // Get users with "User" role
            var usersInUserRole = await _userManager.GetUsersInRoleAsync(AppRoles.User);
            var userIds = usersInUserRole.Select(u => u.Id).ToList();

            // Query users with pagination
            var userQuery = _userManager.Users
                .Where(u => userIds.Contains(u.Id));

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                userQuery = userQuery.Where(u => 
                    u.Email.Contains(query.SearchTerm) || 
                    u.UserName.Contains(query.SearchTerm) || 
                    u.FullName.Contains(query.SearchTerm) ||
                    u.PhoneNumber.Contains(query.SearchTerm));
            }

            // Get subscriptions for filtering
            var subscriptions = await _subscriptionRepository.AsQueryable(false)
                .Include(s => s.Plan)
                .Where(s => s.IsActive && !s.IsPaused && s.EndDate > DateTimeOffset.UtcNow)
                .ToListAsync();

            var subscriptionsByUserId = subscriptions.GroupBy(s => s.UserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.EndDate).First());

            // Apply subscription filter if requested
            List<string> filteredUserIds = userIds;
            if (query.HasActiveSubscription.HasValue)
            {
                if (query.HasActiveSubscription.Value)
                {
                    // Users with active subscriptions
                    filteredUserIds = subscriptionsByUserId.Keys.ToList();
                }
                else
                {
                    // Users without active subscriptions
                    filteredUserIds = userIds.Where(id => !subscriptionsByUserId.ContainsKey(id)).ToList();
                }
                userQuery = userQuery.Where(u => filteredUserIds.Contains(u.Id));
            }

            // Get total count for pagination
            var totalCount = await userQuery.CountAsync();

            // Apply pagination
            var pagedUsers = await userQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // Map to DTOs
            var userDtos = new List<UserSearchDto>();
            foreach (var user in pagedUsers)
            {
                bool hasActiveSubscription = subscriptionsByUserId.ContainsKey(user.Id);
                var subscription = hasActiveSubscription ? subscriptionsByUserId[user.Id] : null;

                userDtos.Add(new UserSearchDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    HasActiveSubscription = hasActiveSubscription,
                    SubscriptionPlanName = hasActiveSubscription ? subscription.Plan.Name : null,
                    SubscriptionEndDate = hasActiveSubscription ? subscription.EndDate : null
                });
            }

            var result = new PaginatedList<UserSearchDto>(
                userDtos,
                totalCount,
                query.PageNumber,
                query.PageSize);

            return ApiResponse<PaginatedList<UserSearchDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return ApiResponse<PaginatedList<UserSearchDto>>.Fail("An error occurred while searching users");
        }
    }

    public async Task<ApiResponse<UserSearchDto>> GetUserWithSubscriptionDetailsAsync(string userId)
    {
        try
        {
            // Verify current user has permission to view this user's details
            var currentUserId = _currentUserService.UserId;
            var isAdmin = _currentUserService.User.IsInRole(AppRoles.Admin) || 
                          _currentUserService.User.IsInRole(AppRoles.SuperAdmin);
            var isClubUser = _currentUserService.User.IsInRole(AppRoles.ClubUser);
            
            if (!isAdmin && !isClubUser)
            {
                return ApiResponse<UserSearchDto>.Fail("Unauthorized access");
            }

            // Get the user details
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserSearchDto>.Fail($"User with ID {userId} not found");
            }

            // Check if the user has the "User" role
            var isInUserRole = await _userManager.IsInRoleAsync(user, AppRoles.User);
            if (!isInUserRole)
            {
                return ApiResponse<UserSearchDto>.Fail($"User with ID {userId} is not a regular user");
            }

            // Get active subscription if any
            var subscription = await _subscriptionRepository.AsQueryable(false)
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.IsActive && !s.IsPaused && s.EndDate > DateTimeOffset.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            bool hasActiveSubscription = subscription != null;

            var userDto = new UserSearchDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                HasActiveSubscription = hasActiveSubscription,
                SubscriptionPlanName = hasActiveSubscription ? subscription.Plan.Name : null,
                SubscriptionEndDate = hasActiveSubscription ? subscription.EndDate : null
            };

            return ApiResponse<UserSearchDto>.Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user with subscription details for user ID {UserId}", userId);
            return ApiResponse<UserSearchDto>.Fail("An error occurred while retrieving user details");
        }
    }
    
    public async Task<ApiResponse<bool>> CheckUserSubscriptionByPhoneAsync(string phoneNumber)
    {
        try
        {
            // Verify current user has permission 
            var isAdmin = _currentUserService.User.IsInRole(AppRoles.Admin) || 
                          _currentUserService.User.IsInRole(AppRoles.SuperAdmin);
            var isClubUser = _currentUserService.User.IsInRole(AppRoles.ClubUser);
            
            if (!isAdmin && !isClubUser)
            {
                return ApiResponse<bool>.Fail("Unauthorized access");
            }
            
            // If it's a ClubUser (not admin), verify they have access to at least one club
            if (!isAdmin && isClubUser)
            {
                var userId = _currentUserService.UserId;
                var hasClubAccess = await _repository.AsQueryable(false)
                    .AnyAsync(cu => cu.UserId == userId && cu.IsActive);
                
                if (!hasClubAccess)
                {
                    return ApiResponse<bool>.Fail("You don't have access to any clubs");
                }
            }

            // Find user by phone number
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                // Return false instead of error when user is not found
                return ApiResponse<bool>.Ok(false, $"No user found with phone number {phoneNumber}");
            }

            // Check if user has the "User" role
            var isInUserRole = await _userManager.IsInRoleAsync(user, AppRoles.User);
            if (!isInUserRole)
            {
                // Return false for non-regular users
                return ApiResponse<bool>.Ok(false, $"The phone number belongs to a non-regular user");
            }

            // Check for active subscription
            var hasActiveSubscription = await _subscriptionRepository.AsQueryable(false)
                .AnyAsync(s => s.UserId == user.Id && 
                             s.IsActive && 
                             !s.IsPaused && 
                             s.EndDate > DateTimeOffset.UtcNow);

            // Return simple boolean result
            return ApiResponse<bool>.Ok(hasActiveSubscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription for user with phone number {PhoneNumber}", phoneNumber);
            return ApiResponse<bool>.Fail("An error occurred while checking the user subscription");
        }
    }
}