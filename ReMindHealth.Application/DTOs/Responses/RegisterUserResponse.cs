namespace ReMindHealth.Application.DTOs.Responses;

public class RegisterUserResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
}

