using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Interfaces.IServices;

public interface IConversationService
{
    Task<Conversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationWithDetailsAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetUserConversationsAsync(CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetRecentConversationsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<Conversation> CreateConversationWithAudioAsync(string? note, byte[] audioData, CancellationToken cancellationToken = default);
    Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task ContinueProcessingFromTranscriptionAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task UpdateTranscriptionTextOnlyAsync(Guid conversationId, string transcriptionText, CancellationToken cancellationToken = default);
}