using ReMindHealth.Data;

namespace ReMindHealth.Models;

public class Conversation
{
    public Guid ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int? AudioDurationSeconds { get; set; }
    public string? AudioFormat { get; set; }
    public string? TranscriptionText { get; set; }
    public string? TranscriptionLanguage { get; set; }
    public string? Summary { get; set; }
    public string ProcessingStatus { get; set; } = "Pending";
    public string? ProcessingError { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public bool IsFavorite { get; set; } = false;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<ExtractedAppointment> ExtractedAppointments { get; set; } = new List<ExtractedAppointment>();
    public ICollection<ExtractedTask> ExtractedTasks { get; set; } = new List<ExtractedTask>();
    public ICollection<ExtractedNote> ExtractedNotes { get; set; } = new List<ExtractedNote>();
}