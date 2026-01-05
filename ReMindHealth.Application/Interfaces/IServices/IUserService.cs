using ReMindHealth.Application.DTOs.Requests;
using ReMindHealth.Application.DTOs.Responses;

namespace ReMindHealth.Application.Interfaces.IServices;

public interface IUserService
{
    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<UserInfoResponse?> GetCurrentUserInfoAsync(CancellationToken cancellationToken = default);
    Task<UserInfoResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<string> GetCurrentUserIdAsync();
    Task<string?> GetCurrentUserEmailAsync();
    Task<string?> GetCurrentUserFullNameAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> AcceptPrivacyPolicyAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateUserProfileAsync(string userId, string firstName, string lastName, CancellationToken cancellationToken = default);

    Task<bool> ResetPasswordAsync(string email, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> DeleteAccountAsync(string userId, CancellationToken cancellationToken = default);
}