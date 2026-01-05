using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Interfaces.IRepositories;

public interface ITaskRepository : IRepository<ExtractedTask>
{
    Task<List<ExtractedTask>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<List<ExtractedTask>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<ExtractedTask>> GetPendingByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}