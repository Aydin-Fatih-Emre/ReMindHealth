using Microsoft.EntityFrameworkCore;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Domain.Models;
using ReMindHealth.Infrastructure.Data;

namespace ReMindHealth.Infrastructure.Repositories.Implementation;

public class NoteRepository : INoteRepository
{
    private readonly ApplicationDbContext _context;

    public NoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExtractedNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedNotes
            .Include(n => n.Conversation)
            .FirstOrDefaultAsync(n => n.NoteId == id, cancellationToken);
    }

    public async Task<List<ExtractedNote>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedNotes
            .Include(n => n.Conversation)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExtractedNote>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedNotes
            .Where(n => n.ConversationId == conversationId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExtractedNote>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedNotes
            .Include(n => n.Conversation)
            .Where(t => t.UserId == userId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExtractedNote>> GetPinnedByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractedNotes
            .Include(n => n.Conversation)
            .Where(t => t.UserId == userId
                     && t.IsPinned)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExtractedNote> AddAsync(ExtractedNote entity, CancellationToken cancellationToken = default)
    {
        entity.NoteId = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.ExtractedNotes.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(ExtractedNote entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.ExtractedNotes.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await GetByIdAsync(id, cancellationToken);
        if (note != null)
        {
            _context.ExtractedNotes.Remove(note);
        }
    }
}