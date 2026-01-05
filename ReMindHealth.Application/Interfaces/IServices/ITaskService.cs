using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Interfaces.IServices;

public interface ITaskService
{
    Task<ExtractedTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<List<ExtractedTask>> GetUserTasksAsync(CancellationToken cancellationToken = default);
    Task<List<ExtractedTask>> GetPendingTasksAsync(CancellationToken cancellationToken = default);
    Task<ExtractedTask> CreateTaskAsync(ExtractedTask task, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(ExtractedTask task, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}