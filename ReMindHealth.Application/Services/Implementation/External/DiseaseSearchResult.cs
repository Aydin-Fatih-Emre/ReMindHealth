namespace ReMindHealth.Application.Services.Implementation.External
{
    public class DiseaseSearchResult
    {
        public string DiseaseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Symptoms { get; set; } = new();
        public List<string> Causes { get; set; } = new();
        public List<string> Treatments { get; set; } = new();
        public List<string> Prevention { get; set; } = new();
        public List<string> WhenToSeeDoctor { get; set; } = new();
        public string AdditionalInfo { get; set; } = string.Empty;
    }
}
