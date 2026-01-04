using ReMindHealth.Data;

namespace ReMindHealth.Models;

public class ExtractedAppointment
{
    public Guid AppointmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? AttendeeNames { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Conversation? Conversation { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}