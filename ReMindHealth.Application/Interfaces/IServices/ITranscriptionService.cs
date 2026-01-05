using ReMindHealth.Application.Services.Implementation.External;

namespace ReMindHealth.Application.Interfaces.IServices;

public interface ITranscriptionService
{
    //Task<TranscriptionResult> TranscribeAsync(
    //    string audioFilePath,
    //    CancellationToken cancellationToken = default);
    Task<TranscriptionResult> TranscribeFromStreamAsync(
        Stream audioStream,
        CancellationToken cancellationToken = default);
}
