using AssemblyAI;
using AssemblyAI.Transcripts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReMindHealth.Application.Interfaces.IServices;

namespace ReMindHealth.Application.Services.Implementation.External;

public class AssemblyAITranscriptionService : ITranscriptionService
{
    private readonly AssemblyAIClient _client;
    private readonly ILogger<AssemblyAITranscriptionService> _logger;

    public AssemblyAITranscriptionService(
        IConfiguration configuration,
        ILogger<AssemblyAITranscriptionService> logger)
    {
        var apiKey = configuration["AssemblyAI:ApiKey"]
            ?? throw new InvalidOperationException("AssemblyAI API key not configured");

        _client = new AssemblyAIClient(apiKey);
        _logger = logger;
    }
    public async Task<TranscriptionResult> TranscribeFromStreamAsync(
        Stream audioStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Transcribing from stream");

            // AssemblyAI SDK supports Stream directly
            var transcript = await _client.Transcripts.TranscribeAsync(
                audioStream,
                new TranscriptOptionalParams
                {
                    LanguageCode = TranscriptLanguageCode.De,
                    Punctuate = true,
                    FormatText = true
                }
            );

            if (transcript.Status == TranscriptStatus.Error)
            {
                throw new Exception($"Transcription failed: {transcript.Error}");
            }

            _logger.LogInformation("Transcription completed successfully");

            return new TranscriptionResult
            {
                Text = transcript.Text ?? string.Empty,
                Language = "de",
                Duration = transcript.AudioDuration / 1000.0,
                Confidence = transcript.Confidence ?? 0.0,
                Words = transcript.Words?.Select(w => new TranscriptionWord
                {
                    Text = w.Text,
                    Start = w.Start / 1000.0,
                    End = w.End / 1000.0,
                    Confidence = w.Confidence
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transcription from stream");
            throw;
        }
    }
}