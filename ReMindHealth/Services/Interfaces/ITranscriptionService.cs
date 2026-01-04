namespace ReMindHealth.Services.Interfaces;

public interface ITranscriptionService
{
    //Task<TranscriptionResult> TranscribeAsync(
    //    string audioFilePath,
    //    CancellationToken cancellationToken = default);
    Task<TranscriptionResult> TranscribeFromStreamAsync(
        Stream audioStream,
        CancellationToken cancellationToken = default);
}
