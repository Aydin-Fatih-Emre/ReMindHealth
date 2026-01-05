using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Interfaces.IRepositories;

public interface INoteRepository : IRepository<ExtractedNote>
{
    Task<List<ExtractedNote>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<List<ExtractedNote>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<ExtractedNote>> GetPinnedByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}