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
using PadelPass.Core.Constants;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
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

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IGenericRepository<RefreshToken> refreshTokenRepository,
        IMapper mapper,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _refreshTokenRepository = refreshTokenRepository;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(
        RegisterDto model)
    {
        try
        {
            // Check if user exists
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return ApiResponse<AuthResponseDto>.Fail("User with this email already exists");
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
            return ApiResponse<AuthResponseDto>.Fail("An error occurred during user registration");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
        LoginDto model)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password");
            }

            // Verify password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password");
            }

            // Generate JWT token
            return await GenerateAuthResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return ApiResponse<AuthResponseDto>.Fail("An error occurred during login");
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
                return ApiResponse<AuthResponseDto>.Fail("Invalid refresh token");
            }

            // Check if token is expired
            if (refreshToken.ExpiryDate < DateTime.UtcNow)
            {
                return ApiResponse<AuthResponseDto>.Fail("Refresh token has expired");
            }

            // Get user
            var user = await _userManager.FindByIdAsync(refreshToken.UserId);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("User not found");
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
            return ApiResponse<AuthResponseDto>.Fail("An error occurred while refreshing the token");
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
                return ApiResponse<UserDto>.Fail("User with this email already exists");
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

            return ApiResponse<UserDto>.Ok(userDto, "Admin user created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user");
            return ApiResponse<UserDto>.Fail("An error occurred while creating the admin user");
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
                return ApiResponse<UserDto>.Fail($"User with ID {userId} not found");
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
            return ApiResponse<UserDto>.Fail("An error occurred while retrieving the user");
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
            return ApiResponse<List<UserDto>>.Fail("An error occurred while retrieving users");
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
            return ApiResponse<bool>.Ok(true, "Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return ApiResponse<bool>.Fail("An error occurred during logout");
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
            ExpiryDate = DateTime.UtcNow.AddDays(7),
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
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
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