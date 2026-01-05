using Microsoft.EntityFrameworkCore;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Domain.Models;
using ReMindHealth.Infrastructure.Data;

namespace ReMindHealth.Infrastructure.Repositories.Implementation;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _context;

    public ConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _context.Conversations
            .FirstOrDefaultAsync(c => c.ConversationId == id && !c.IsDeleted, cancellationToken);

        if (result == null)
        {
            var exists = await _context.Conversations
                .AnyAsync(c => c.ConversationId == id, cancellationToken);

            Console.WriteLine($"Conversation {id} - Exists: {exists}, Result: {result != null}");
        }

        return result;
    }

    public async Task<Conversation?> GetWithDetailsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.ExtractedAppointments)
            .Include(c => c.ExtractedTasks)
            .Include(c => c.ExtractedNotes)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId && !c.IsDeleted, cancellationToken);
    }

    public async Task<List<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Conversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Conversation>> GetRecentByUserIdAsync(string userId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.RecordedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation> AddAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.Conversations.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(Conversation entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Conversations.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conversation = await GetByIdAsync(id, cancellationToken);
        if (conversation != null)
        {
            conversation.IsDeleted = true;
            conversation.UpdatedAt = DateTime.UtcNow;
        }
    }
}