namespace ReMindHealth.Models;

public class ExtractedNote
{
    public Guid NoteId { get; set; }
    public Guid ConversationId { get; set; }
    public string NoteType { get; set; } = "General";
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Conversation Conversation { get; set; } = null!;
}