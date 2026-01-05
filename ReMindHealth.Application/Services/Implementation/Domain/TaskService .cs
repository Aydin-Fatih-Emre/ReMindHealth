using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Services.Implementation.Domain;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;

    public TaskService(
        IUnitOfWork unitOfWork,
        IUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = currentUserService;
    }

    public Task<ExtractedTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.TaskRepository.GetByIdAsync(taskId, cancellationToken);
    }

    public async Task<List<ExtractedTask>> GetUserTasksAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.TaskRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<ExtractedTask>> GetPendingTasksAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.TaskRepository.GetPendingByUserIdAsync(userId, cancellationToken);
    }

    public async Task<ExtractedTask> CreateTaskAsync(ExtractedTask task, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.TaskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveAsync();
        return task;
    }

    public async Task UpdateTaskAsync(ExtractedTask task, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.TaskRepository.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveAsync();
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.TaskRepository.DeleteAsync(taskId, cancellationToken);
        await _unitOfWork.SaveAsync();
    }
}