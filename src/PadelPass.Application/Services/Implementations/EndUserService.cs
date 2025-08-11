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
    public class EndUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGenericRepository<ClubUser> _clubUserRepository;
        private readonly IGenericRepository<Subscription> _subscriptionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<EndUserService> _logger;
        private readonly IGlobalLocalizer _localizer;

        public EndUserService(
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IGenericRepository<ClubUser> clubUserRepository,
            IGenericRepository<Subscription> subscriptionRepository,
            IMapper mapper,
            IGlobalLocalizer localizer,
            ILogger<EndUserService> logger)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _clubUserRepository = clubUserRepository;
            _subscriptionRepository = subscriptionRepository;
            _mapper = mapper;
            _localizer = localizer;
            _logger = logger;
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
                    accessibleClubIds = await _clubUserRepository.AsQueryable(false)
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
    }
}

