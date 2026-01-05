using ReMindHealth.Application.Services.Implementation.External;

namespace ReMindHealth.Application.Interfaces.IServices
{
    public interface IDiseaseSearchService
    {
        Task<DiseaseSearchResult> SearchDiseaseAsync(string diseaseName, CancellationToken cancellationToken = default);
    }
}
