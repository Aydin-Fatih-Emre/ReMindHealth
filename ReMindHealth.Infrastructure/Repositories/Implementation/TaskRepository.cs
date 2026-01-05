using Microsoft.EntityFrameworkCore;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Domain.Models;
using ReMindHealth.Infrastructure.Data;

namespace ReMindHealth.Infrastructure.Repositories.Implementation;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExtractedTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedTasks
            .Include(t => t.Conversation)
            .FirstOrDefaultAsync(t => t.TaskId == id, cancellationToken);
    }

    public async Task<List<ExtractedTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedTasks
            .Include(t => t.Conversation)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExtractedTask>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedTasks
            .Where(t => t.ConversationId == conversationId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExtractedTask>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedTasks
            .Include(t => t.Conversation)
            .Where(t => t.UserId == userId) 
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExtractedTask>> GetPendingByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedTasks
            .Include(t => t.Conversation)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExtractedTask> AddAsync(ExtractedTask entity, CancellationToken cancellationToken = default)
    {
        entity.TaskId = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.ExtractedTasks.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(ExtractedTask entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.IsCompleted && !entity.CompletedAt.HasValue)
        {
            entity.CompletedAt = DateTime.UtcNow;
        }

        _context.ExtractedTasks.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetByIdAsync(id, cancellationToken);
        if (task != null)
        {
            _context.ExtractedTasks.Remove(task);
        }
    }
}