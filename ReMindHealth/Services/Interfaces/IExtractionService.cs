using ReMindHealth.Services.Implementations;

namespace ReMindHealth.Services.Interfaces;

public interface IExtractionService
{
    Task<ExtractionResult> ExtractInformationAsync(
        string transcriptionText,
        string userId,
        CancellationToken cancellationToken = default);
}