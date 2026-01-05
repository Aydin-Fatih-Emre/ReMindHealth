using ReMindHealth.Application.Services.Implementation.External;

namespace ReMindHealth.Application.Interfaces.IServices;

public interface IExtractionService
{
    Task<ExtractionResult> ExtractInformationAsync(
        string transcriptionText,
        string userId,
        CancellationToken cancellationToken = default);
}