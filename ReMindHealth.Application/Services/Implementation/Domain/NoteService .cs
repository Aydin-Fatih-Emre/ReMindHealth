using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Services.Implementation.Domain;

public class NoteService : INoteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;

    public NoteService(
        IUnitOfWork unitOfWork,
        IUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = currentUserService;
    }

    public Task<ExtractedNote?> GetNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.NoteRepository.GetByIdAsync(noteId, cancellationToken);
    }

    public async Task<List<ExtractedNote>> GetUserNotesAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.NoteRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<ExtractedNote>> GetPinnedNotesAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.NoteRepository.GetPinnedByUserIdAsync(userId, cancellationToken);
    }

    public async Task<ExtractedNote> CreateNoteAsync(ExtractedNote note, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.NoteRepository.AddAsync(note, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return note;
    }

    public async Task UpdateNoteAsync(ExtractedNote note, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.NoteRepository.UpdateAsync(note, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task DeleteNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.NoteRepository.DeleteAsync(noteId, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}