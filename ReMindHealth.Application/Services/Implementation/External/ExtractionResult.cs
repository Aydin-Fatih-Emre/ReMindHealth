using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Services.Implementation.External
{
    public class ExtractionResult
    {
        public string? Summary { get; set; }
        public string? CorrectedTranscription { get; set; }
        public List<ExtractedAppointment> Appointments { get; set; } = new();
        public List<ExtractedTask> Tasks { get; set; } = new();
        public List<ExtractedNote> Notes { get; set; } = new();
    }
}
