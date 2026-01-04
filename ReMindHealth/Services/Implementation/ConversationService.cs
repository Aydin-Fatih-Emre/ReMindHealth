using Microsoft.EntityFrameworkCore;
using ReMindHealth.Data;
using ReMindHealth.Models;
using ReMindHealth.Services.Interfaces;

namespace ReMindHealth.Services.Implementations;

public class ConversationService : IConversationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExtractionService _extractionService;
    private readonly ILogger<ConversationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConversationService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IExtractionService extractionService,
        ILogger<ConversationService> logger,
        IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _extractionService = extractionService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // ===================================
    // QUERY METHODS - Using Repository
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
        var userId = await _currentUserService.GetUserIdAsync();
        return await _unitOfWork.ConversationRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<Conversation>> GetRecentConversationsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var userId = await _currentUserService.GetUserIdAsync();
        return await _unitOfWork.ConversationRepository.GetRecentByUserIdAsync(userId, count, cancellationToken);
    }

    // ===================================
    // COMMAND METHODS - Using Repository
    //===================================

    public async Task<Conversation> CreateConversationAsync(string? title, string? conversationType = null, CancellationToken cancellationToken = default)
    {
        var userId = await _currentUserService.GetUserIdAsync();

        var conversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            UserId = userId,
            Title = title ?? $"Gespräch vom {DateTime.Now:dd.MM.yyyy HH:mm}",
            RecordedAt = DateTime.UtcNow,
            ProcessingStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ✅ Using repository
        await _unitOfWork.ConversationRepository.AddAsync(conversation, cancellationToken);

        return conversation;
    }

    public async Task<Conversation> CreateConversationWithAudioAsync(
        string? note,
        byte[] audioData,
        CancellationToken cancellationToken = default)
    {
        var userId = await _currentUserService.GetUserIdAsync();
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

        // ✅ Using repository
        await _unitOfWork.ConversationRepository.AddAsync(conversation, cancellationToken);

        // Pass audio data directly to transcription in background
        _ = Task.Run(async () =>
        {
            try
            {
                await TranscribeFromMemoryAsync(conversationId, audioData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background transcription task");
            }
        });

        return conversation;
    }

    public async Task UpdateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        conversation.UpdatedAt = DateTime.UtcNow;

        // ✅ Using repository
        await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
    }

    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        // ✅ Using repository
        var conversation = await _unitOfWork.ConversationRepository.GetByIdAsync(conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.IsDeleted = true;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
        }
    }

    public async Task UpdateTranscriptionTextOnlyAsync(Guid conversationId, string transcriptionText, CancellationToken cancellationToken = default)
    {
        // ✅ Using repository
        var conversation = await _unitOfWork.ConversationRepository.GetByIdAsync(conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.TranscriptionText = transcriptionText;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.ConversationRepository.UpdateAsync(conversation, cancellationToken);
        }
    }

    // ===================================
    // BACKGROUND PROCESSING
    // ===================================

    public async Task ContinueProcessingFromTranscriptionAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () => await ExtractFromTranscriptionAsync(conversationId));
    }

    private async Task TranscribeFromMemoryAsync(Guid conversationId, byte[] audioData)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var transcriptionService = scope.ServiceProvider.GetRequiredService<ITranscriptionService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConversationService>>();

        try
        {
            // ✅ Using repository through scoped UnitOfWork
            var conversation = await unitOfWork.ConversationRepository.GetByIdAsync(conversationId);

            if (conversation == null)
            {
                logger.LogWarning("Conversation {ConversationId} not found for transcription", conversationId);
                return;
            }

            conversation.ProcessingStatus = "Transcribing";
            await unitOfWork.ConversationRepository.UpdateAsync(conversation);

            // Transcribe directly from memory
            using var memoryStream = new MemoryStream(audioData);
            var transcription = await transcriptionService.TranscribeFromStreamAsync(memoryStream);

            conversation.TranscriptionText = transcription.Text;
            conversation.TranscriptionLanguage = transcription.Language;
            conversation.ProcessingStatus = "Transcribed";
            await unitOfWork.ConversationRepository.UpdateAsync(conversation);

            logger.LogInformation(
                "Transcription completed for conversation {ConversationId}. Confidence: {Confidence}",
                conversationId,
                transcription.Confidence);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error transcribing conversation {ConversationId}", conversationId);

            var conversation = await unitOfWork.ConversationRepository.GetByIdAsync(conversationId);

            if (conversation != null)
            {
                conversation.ProcessingStatus = "Failed";
                conversation.ProcessingError = ex.Message;
                await unitOfWork.ConversationRepository.UpdateAsync(conversation);
            }
        }
    }

    private async Task ExtractFromTranscriptionAsync(Guid conversationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IExtractionService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConversationService>>();

        try
        {
            // ✅ Using repository
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

            // ✅ Add extracted items using repositories
            foreach (var apt in extraction.Appointments)
            {
                apt.ConversationId = conversationId;
                await unitOfWork.AppointmentRepository.AddAsync(apt);
            }

            foreach (var task in extraction.Tasks)
            {
                task.ConversationId = conversationId;
                await unitOfWork.TaskRepository.AddAsync(task);
            }

            foreach (var note in extraction.Notes)
            {
                note.ConversationId = conversationId;
                await unitOfWork.NoteRepository.AddAsync(note);
            }

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