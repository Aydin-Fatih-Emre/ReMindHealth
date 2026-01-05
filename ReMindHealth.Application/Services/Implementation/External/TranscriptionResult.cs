namespace ReMindHealth.Application.Services.Implementation.External
{
    public class TranscriptionResult
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = "de";
        public double? Duration { get; set; }
        public double Confidence { get; set; }
        public List<TranscriptionWord>? Words { get; set; }
    }
}
