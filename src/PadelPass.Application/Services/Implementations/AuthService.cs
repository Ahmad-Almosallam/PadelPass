using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PadelPass.Application.DTOs.Authentication;
using PadelPass.Core.Common;
using PadelPass.Core.Common.Enums;
using PadelPass.Core.Constants;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace PadelPass.Application.Services.Implementations;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IGenericRepository<RefreshToken> _refreshTokenRepository;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IGlobalLocalizer _localizer;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IGenericRepository<RefreshToken> refreshTokenRepository,
        IMapper mapper,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IGlobalLocalizer localizer)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _refreshTokenRepository = refreshTokenRepository;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(
        RegisterDto model)
    {
        try
        {
            // Check if user exists
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                return ApiResponse<AuthResponseDto>.Fail(_localizer["UserWithEmailExists"]);
            }
            
            var userExists = await _userManager.Users.AnyAsync(x => x.PhoneNumber == model.PhoneNumber);
            if (userExists)
            {
                return ApiResponse<AuthResponseDto>.Fail(
                    _localizer["UserWithPhoneNumberExists", model.PhoneNumber]
                );
            }

            // Create user
            user = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                UserType = UserType.EndUser,
                SecurityStamp = Guid.NewGuid()
                    .ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return ApiResponse<AuthResponseDto>.Fail(errors);
            }

            // Ensure User role exists
            if (!await _roleManager.RoleExistsAsync(AppRoles.User))
            {
                await _roleManager.CreateAsync(new IdentityRole(AppRoles.User));
            }

            // Assign User role
            await _userManager.AddToRoleAsync(user, AppRoles.User);

            // Generate JWT token
            return await GenerateAuthResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return ApiResponse<AuthResponseDto>.Fail(_localizer["ErrorOccurredDuringRegistration"]);
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
        LoginDto model)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.Fail(_localizer["InvalidEmailOrPassword"]);
            }

            // Verify password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                return ApiResponse<AuthResponseDto>.Fail(_localizer["InvalidEmailOrPassword"]);
            }

            // Generate JWT token
            return await GenerateAuthResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return ApiResponse<AuthResponseDto>.Fail(_localizer["ErrorOccurredDuringLogin"]);
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenDto model)
    {
        try
        {
            // Find refresh token
            var refreshToken = await _refreshTokenRepository.AsQueryable(false)
                .FirstOrDefaultAsync(r => r.Token == model.RefreshToken && !r.IsRevoked && !r.IsUsed);

            if (refreshToken == null)
            {
                return ApiResponse<AuthResponseDto>.Fail(_localizer["InvalidRefreshToken"]);
            }

            // Check if token is expired
            if (refreshToken.ExpiryDate < DateTimeOffset.UtcNow)
            {
                return ApiResponse<AuthResponseDto>.Fail(_localizer["RefreshTokenExpired"]);
            }

            // Get user
            var user = await _userManager.FindByIdAsync(refreshToken.UserId);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail(_localizer["UserNotFound"]);
            }

            // Mark current token as used
            refreshToken.IsUsed = true;
            _refreshTokenRepository.Update(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            // Generate new JWT token
            return await GenerateAuthResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return ApiResponse<AuthResponseDto>.Fail(_localizer["ErrorOccurredWhileRefreshingToken"]);
        }
    }

    public async Task<ApiResponse<UserDto>> CreateAdminAsync(
        CreateAdminDto model)
    {
        try
        {
            // Check if user exists
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return ApiResponse<UserDto>.Fail(_localizer["UserWithEmailExists"]);
            }

            // Create user
            var user = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                SecurityStamp = Guid.NewGuid()
                    .ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return ApiResponse<UserDto>.Fail(errors);
            }

            // Ensure Admin role exists
            if (!await _roleManager.RoleExistsAsync(AppRoles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
            }

            // Assign Admin role
            await _userManager.AddToRoleAsync(user, AppRoles.Admin);

            // Return user DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                Roles = new List<string> { AppRoles.Admin }
            };

            return ApiResponse<UserDto>.Ok(userDto, _localizer["AdminUserCreatedSuccessfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user");
            return ApiResponse<UserDto>.Fail(_localizer["ErrorOccurredWhileCreatingAdmin"]);
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(
        string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserDto>.Fail(_localizer["UserNotFound"]);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                Roles = roles.ToList()
            };

            return ApiResponse<UserDto>.Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user with ID {UserId}", userId);
            return ApiResponse<UserDto>.Fail(_localizer["ErrorOccurredWhileRetrieving", _localizer["User"]]);
        }
    }

    public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FullName = user.FullName,
                    Roles = roles.ToList()
                });
            }

            return ApiResponse<List<UserDto>>.Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return ApiResponse<List<UserDto>>.Fail(_localizer["ErrorOccurredWhileRetrieving", "users"]);
        }
    }

    public async Task<ApiResponse<bool>> LogoutAsync(
        string userId)
    {
        try
        {
            // Invalidate all refresh tokens for the user
            var refreshTokens = await _refreshTokenRepository.AsQueryable(false)
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                _refreshTokenRepository.Update(token);
            }

            await _refreshTokenRepository.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, _localizer["LogoutSuccessful"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return ApiResponse<bool>.Fail(_localizer["ErrorOccurredDuringLogout"]);
        }
    }

    private async Task<ApiResponse<AuthResponseDto>> GenerateAuthResponseAsync(
        ApplicationUser user)
    {
        // Get user roles
        var userRoles = await _userManager.GetRolesAsync(user);

        // Create claims
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid()
                .ToString())
        };

        // Add role claims
        foreach (var role in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Generate JWT token
        var token = GenerateJwtToken(authClaims);

        // Generate refresh token
        var refreshToken = GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            JwtId = authClaims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)
                ?.Value,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            IsUsed = false
        };

        _refreshTokenRepository.Insert(refreshTokenEntity);
        await _refreshTokenRepository.SaveChangesAsync();

        // Return response
        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            Roles = userRoles.ToList()
        });
    }

    private string GenerateJwtToken(
        List<Claim> claims)
    {
        var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}