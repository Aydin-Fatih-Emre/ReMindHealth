namespace ReMindHealth.Models;

public class ExtractedTask
{
    public Guid TaskId { get; set; }
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Conversation Conversation { get; set; } = null!;
}