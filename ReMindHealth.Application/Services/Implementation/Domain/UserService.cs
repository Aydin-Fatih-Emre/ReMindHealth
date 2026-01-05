using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ReMindHealth.Application.DTOs.Requests;
using ReMindHealth.Application.DTOs.Responses;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Services.Implementation.Domain;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        SignInManager<ApplicationUser> signInManager,
        AuthenticationStateProvider authenticationStateProvider,
        UserManager<ApplicationUser> userManager,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _signInManager = signInManager;
        _authenticationStateProvider = authenticationStateProvider;
        _userManager = userManager;
        _logger = logger;
    }

    // ===================================
    // Registration & User Management
    // ===================================

    public async Task<RegisterUserResponse> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true, 
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                HasAcceptedPrivacy = false
            };

            await _userRepository.CreateAsync(user, request.Password, cancellationToken);


            _logger.LogInformation("User {Email} registered successfully", request.Email);

            return new RegisterUserResponse
            {
                Success = true,
                UserId = user.Id,
                Email = user.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", request.Email);

            return new RegisterUserResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<UserInfoResponse?> GetCurrentUserInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            return await GetUserByIdAsync(userId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public async Task<UserInfoResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
            return null;

        return new UserInfoResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            HasAcceptedPrivacy = user.HasAcceptedPrivacy,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdateUserProfileAsync(
        string userId,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return false;

            user.FirstName = firstName;
            user.LastName = lastName;

            await _userRepository.UpdateAsync(user, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for {UserId}", userId);
            return false;
        }
    }
    public async Task<bool> ResetPasswordAsync(
     string email,
     string newPassword,
     CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", email);
                return false;
            }

            var resetToken = await _userRepository.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Password reset failed for {Email}: {Errors}",
                    email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            _logger.LogInformation("Password reset successful for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", email);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(
    string userId,
    string oldPassword,
    string newPassword,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return false;

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Password change failed for {UserId}: {Errors}",
                    userId,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            await _signInManager.RefreshSignInAsync(user);

            _logger.LogInformation("Password changed successfully for {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DeleteAccountAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
                return false;
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Account deletion failed for {UserId}: {Errors}",
                    userId,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }


            _logger.LogInformation("Account deleted for {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for {UserId}", userId);
            return false;
        }
    }
    public async Task<bool> AcceptPrivacyPolicyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return false;

            user.HasAcceptedPrivacy = true;
            user.PrivacyAcceptedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to accept privacy: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            _logger.LogInformation("User {UserId} accepted privacy policy", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting privacy policy");
            return false;
        }
    }

    // ===================================
    // Current User Helpers (from ICurrentUserService)
    // ===================================

    public async Task<string> GetCurrentUserIdAsync()
    {
        var user = await GetCurrentUserAsync();

        if (user == null)
            throw new UnauthorizedAccessException("User not authenticated");

        return user.Id;
    }

    public async Task<string?> GetCurrentUserEmailAsync()
    {
        var user = await GetCurrentUserAsync();
        return user?.Email;
    }

    public async Task<string?> GetCurrentUserFullNameAsync()
    {
        var user = await GetCurrentUserAsync();

        if (user == null)
            return null;

        return $"{user.FirstName} {user.LastName}".Trim();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }

    // ===================================
    // Private Helper
    // ===================================

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return await _userManager.GetUserAsync(authState.User);
    }
}