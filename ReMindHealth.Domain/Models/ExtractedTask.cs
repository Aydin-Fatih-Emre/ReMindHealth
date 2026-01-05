namespace ReMindHealth.Domain.Models;

public class ExtractedTask
{
    public Guid TaskId { get; set; }
    public string UserId { get; set; } = string.Empty; 
    public Guid? ConversationId { get; set; } 
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Conversation? Conversation { get; set; }
}