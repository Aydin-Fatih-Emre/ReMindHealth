using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Services.Implementation.Domain;

public class ConversationService : IConversationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly IExtractionService _extractionService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<ConversationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConversationService(
        IUnitOfWork unitOfWork,
        IUserService currentUserService,
        IExtractionService extractionService,
        ITranscriptionService transcriptionService,
        ILogger<ConversationService> logger,
        IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _userService = currentUserService;
        _extractionService = extractionService;
        _transcriptionService = transcriptionService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // ===================================
    // QUERY METHODS 
    // ===================================

    public Task<Conversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.ConversationRepository.GetByIdAsync(conversationId, cancellationToken);
    }

    public Task<Conversation?> GetConversationWithDetailsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.ConversationRepository.GetWithDetailsAsync(conversationId, cancellationToken);
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.ConversationRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<Conversation>> GetRecentConversationsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.ConversationRepository.GetRecentByUserIdAsync(userId, count, cancellationToken);
    }

    // ===================================
    // COMMAND METHODS 
    //===================================

    public async Task<Conversation> CreateConversationWithAudioAsync(
        string? note,
        byte[] audioData,
        CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        var conversationId = Guid.NewGuid();

        var conversation = new Conversation
        {
            ConversationId = conversationId,
            UserId = userId,
            Title = note ?? $"Gespräch vom {DateTime.Now:dd.MM.yyyy HH:mm}",
            AudioFormat = "webm",
            AudioDurationSeconds = EstimateAudioDuration(audioData),
            RecordedAt = DateTime.UtcNow,
            ProcessingStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ConversationRepository.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Conversation {ConversationId} saved to database", conversationId);

        // Transcribe synchronously 
        try
        {
            conversation.ProcessingStatus = "Transcribing";
            await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
            await _unitOfWork.SaveAsync();

            // Transcribe directly from memory
            using var memoryStream = new MemoryStream(audioData);
            var transcription = await _transcriptionService.TranscribeFromStreamAsync(memoryStream, cancellationToken);

            conversation.TranscriptionText = transcription.Text;
            conversation.TranscriptionLanguage = transcription.Language;
            conversation.ProcessingStatus = "Transcribed";
            await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Transcription completed for conversation {ConversationId}. Confidence: {Confidence}",
                conversationId,
                transcription.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing conversation {ConversationId}", conversationId);

            conversation.ProcessingStatus = "Failed";
            conversation.ProcessingError = ex.Message;
            await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
            await _unitOfWork.SaveAsync();
        }

        return conversation;
    }

    public async Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        conversation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
        await _unitOfWork.SaveAsync();
    }

    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _unitOfWork.ConversationRepository.GetByIdAsync(conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.IsDeleted = true;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
            await _unitOfWork.SaveAsync();
        }
    }

    public async Task UpdateTranscriptionTextOnlyAsync(Guid conversationId, string transcriptionText, CancellationToken cancellationToken = default)
    {
        var conversation = await _unitOfWork.ConversationRepository.GetByIdAsync(conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.TranscriptionText = transcriptionText;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
            await _unitOfWork.SaveAsync();
        }
    }

    // ===================================
    // BACKGROUND PROCESSING
    // ===================================

    public async Task ContinueProcessingFromTranscriptionAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () => await ExtractFromTranscriptionAsync(conversationId));
    }

    private async Task ExtractFromTranscriptionAsync(Guid conversationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IExtractionService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConversationService>>();

        try
        {
            var conversation = await unitOfWork.ConversationRepository.GetByIdAsync(conversationId);

            if (conversation == null || string.IsNullOrEmpty(conversation.TranscriptionText))
            {
                logger.LogWarning("Cannot extract - conversation or transcription not found");
                return;
            }

            conversation.ProcessingStatus = "Analyzing";
            await unitOfWork.ConversationRepository.UpdateAsync(conversation);

            var extraction = await extractionService.ExtractInformationAsync(
                conversation.TranscriptionText,
                conversation.UserId);

            // Update conversation with summary
            conversation.Summary = extraction.Summary ?? string.Empty;
            conversation.ProcessingStatus = "Completed";
            conversation.ProcessedAt = DateTime.UtcNow;

            // Update corrected transcription if provided
            if (!string.IsNullOrEmpty(extraction.CorrectedTranscription))
            {
                conversation.TranscriptionText = extraction.CorrectedTranscription;
                logger.LogInformation("Transcription corrected for conversation {ConversationId}", conversationId);
            }

            await unitOfWork.ConversationRepository.UpdateAsync(conversation);

            foreach (var apt in extraction.Appointments)
            {
                apt.ConversationId = conversationId;
                apt.UserId = conversation.UserId;
                await unitOfWork.AppointmentRepository.AddAsync(apt);
            }

            foreach (var task in extraction.Tasks)
            {
                task.ConversationId = conversationId;
                task.UserId = conversation.UserId;
                await unitOfWork.TaskRepository.AddAsync(task);
            }

            foreach (var note in extraction.Notes)
            {
                note.ConversationId = conversationId;
                note.UserId = conversation.UserId;
                await unitOfWork.NoteRepository.AddAsync(note);
            }

            await unitOfWork.SaveAsync();

            logger.LogInformation(
                "Extraction completed for conversation {ConversationId}: {AppointmentCount} appointments, {TaskCount} tasks, {NoteCount} notes",
                conversationId, extraction.Appointments.Count, extraction.Tasks.Count, extraction.Notes.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting from conversation {ConversationId}", conversationId);

            try
            {
                var conversation = await unitOfWork.ConversationRepository.GetByIdAsync(conversationId);

                if (conversation != null)
                {
                    conversation.ProcessingStatus = "Failed";
                    conversation.ProcessingError = ex.Message;
                    await unitOfWork.ConversationRepository.UpdateAsync(conversation);
                    await unitOfWork.SaveAsync();
                }
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx, "Error saving failure status for conversation {ConversationId}", conversationId);
            }
        }
    }

    private int EstimateAudioDuration(byte[] audioData)
    {
        // Rough estimation: 1 second of WebM audio ≈ 16KB
        return Math.Max(1, audioData.Length / 16000);
    }
}