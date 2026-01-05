namespace ReMindHealth.Domain.Models;

public class ExtractedNote
{
    public Guid NoteId { get; set; }
    public string UserId { get; set; } = string.Empty; 
    public Guid? ConversationId { get; set; } 
    public string NoteType { get; set; } = "General";
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Conversation? Conversation { get; set; }
}