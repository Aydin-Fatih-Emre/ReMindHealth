namespace ReMindHealth.Services.Interfaces;

public interface ICurrentUserService
{
    Task<string> GetUserIdAsync();
}