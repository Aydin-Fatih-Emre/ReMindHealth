using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Interfaces.IServices;

public interface INoteService
{
    Task<ExtractedNote?> GetNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
    Task<List<ExtractedNote>> GetUserNotesAsync(CancellationToken cancellationToken = default);
    Task<List<ExtractedNote>> GetPinnedNotesAsync(CancellationToken cancellationToken = default);
    Task<ExtractedNote> CreateNoteAsync(ExtractedNote note, CancellationToken cancellationToken = default);
    Task UpdateNoteAsync(ExtractedNote note, CancellationToken cancellationToken = default);
    Task DeleteNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
}