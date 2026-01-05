namespace ReMindHealth.Application.DTOs.Responses;

    public class UserInfoResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool HasAcceptedPrivacy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
