using Microsoft.AspNetCore.Identity;

namespace ReMindHealth.Domain.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool HasAcceptedPrivacy { get; set; } = false;
    public DateTime? PrivacyAcceptedAt { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}