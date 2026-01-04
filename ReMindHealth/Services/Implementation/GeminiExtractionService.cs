using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReMindHealth.Models;
using ReMindHealth.Services.Interfaces;

namespace ReMindHealth.Services.Implementations;

public class GeminiExtractionService : IExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiExtractionService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiExtractionService(
        IConfiguration configuration,
        ILogger<GeminiExtractionService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured");

        _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<ExtractionResult> ExtractInformationAsync(
        string transcriptionText,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting information extraction with Gemini model: {Model}", _model);

            var prompt = BuildExtractionPrompt(transcriptionText);
            var response = await CallGeminiApiAsync(prompt, cancellationToken);

            _logger.LogInformation("Gemini response received: {Length} characters", response?.Length ?? 0);

            var result = ParseExtractionResponse(response ?? string.Empty, userId);

            _logger.LogInformation(
                "Extraction completed: {Appointments} appointments, {Tasks} tasks, {Notes} notes",
                result.Appointments.Count,
                result.Tasks.Count,
                result.Notes.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Gemini extraction");

            // Return empty result instead of throwing
            return new ExtractionResult
            {
                Summary = $"Fehler bei der Verarbeitung: {ex.Message}"
            };
        }
    }

    private async Task<string> CallGeminiApiAsync(string prompt, CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1/models/{_model}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 8192,
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling Gemini API v1 with model: {Model}", _model);

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseText);
            _logger.LogWarning("Trying v1beta API as fallback...");
            url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            response = await _httpClient.PostAsync(url, content, cancellationToken);
            responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API error: {response.StatusCode} - {responseText}");
            }
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseText);
        var generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(generatedText))
        {
            throw new Exception("No text generated from Gemini API");
        }

        return generatedText;
    }

    private string BuildExtractionPrompt(string transcriptionText)
    {
        return $@"Du bist ein intelligenter Assistent, der medizinische Gespräche analysiert und strukturierte Informationen extrahiert.

Analysiere den folgenden Transkriptionstext eines medizinischen Gesprächs und extrahiere:
1. **Termine** (Appointments): Alle erwähnten Termine mit Datum, Uhrzeit, Ort und Beschreibung
2. **Aufgaben** (Tasks): Alle zu erledigenden Aufgaben (z.B. Rezept abholen, Anrufe tätigen)
3. **Notizen** (Notes): Wichtige Informationen wie Medikationen, Diagnosen, Warnungen

**WICHTIG:**
- Datumsangaben: Heutiges Datum ist {DateTime.Now:dd.MM.yyyy}
- Relative Zeitangaben umrechnen (z.B. ""in 3 Monaten"" → konkretes Datum)
- Für Termine ohne Uhrzeit: 09:00 Uhr annehmen
- Prioritäten für Aufgaben: Low, Medium, High, Urgent
- Notiztypen: General, Important, Warning, Medication

Antworte NUR mit folgendem JSON-Format (keine zusätzlichen Texte!):

{{
  ""summary"": ""Kurze Zusammenfassung des Gesprächs (2-3 Sätze)"",
  ""correctedTranscription"": null,
  ""appointments"": [
    {{
      ""title"": ""Titel des Termins"",
      ""description"": ""Beschreibung"",
      ""appointmentDateTime"": ""2025-05-15T14:00:00"",
      ""location"": ""Ort""
    }}
  ],
  ""tasks"": [
    {{
      ""title"": ""Titel der Aufgabe"",
      ""description"": ""Beschreibung"",
      ""dueDate"": ""2025-05-10T00:00:00"",
      ""priority"": ""High""
    }}
  ],
  ""notes"": [
    {{
      ""title"": ""Titel der Notiz"",
      ""content"": ""Inhalt der Notiz"",
      ""noteType"": ""Medication"",
      ""isPinned"": true
    }}
  ]
}}

**Transkription:**
{transcriptionText}

**Antwort (nur JSON):**";
    }

    private ExtractionResult ParseExtractionResponse(string responseText, string userId)
    {
        try
        {
            var jsonText = responseText.Trim();
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }
            jsonText = jsonText.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var parsed = JsonSerializer.Deserialize<GeminiResponse>(jsonText, options);

            if (parsed == null)
            {
                _logger.LogWarning("Failed to parse Gemini response, returning empty result");
                return new ExtractionResult();
            }

            return new ExtractionResult
            {
                Summary = parsed.Summary,
                CorrectedTranscription = parsed.CorrectedTranscription,
                Appointments = parsed.Appointments.Select(a => new ExtractedAppointment
                {
                    AppointmentId = Guid.NewGuid(),
                    UserId = userId,
                    Title = a.Title ?? "Unbenannter Termin",
                    Description = a.Description,
                    AppointmentDateTime = DateTime.SpecifyKind(a.AppointmentDateTime, DateTimeKind.Local).ToUniversalTime(),
                    Location = a.Location,
                    CreatedAt = DateTime.UtcNow
                }).ToList(),
                Tasks = parsed.Tasks.Select(t => new ExtractedTask
                {
                    TaskId = Guid.NewGuid(),
                    Title = t.Title ?? "Unbenannte Aufgabe",
                    Description = t.Description,
                    DueDate = t.DueDate?.ToUniversalTime(),
                    Priority = t.Priority ?? "Medium",
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList(),
                Notes = parsed.Notes.Select(n => new ExtractedNote
                {
                    NoteId = Guid.NewGuid(),
                    Title = n.Title ?? "Notiz",
                    Content = n.Content ?? string.Empty,
                    NoteType = n.NoteType ?? "General",
                    IsPinned = n.IsPinned,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList()
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response from Gemini: {Response}", responseText);
            return new ExtractionResult
            {
                Summary = "Fehler beim Parsen der KI-Antwort"
            };
        }
    }

    // DTOs
    private class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class GeminiResponse
    {
        public string? Summary { get; set; }
        public string? CorrectedTranscription { get; set; }
        public List<GeminiAppointment> Appointments { get; set; } = new();
        public List<GeminiTask> Tasks { get; set; } = new();
        public List<GeminiNote> Notes { get; set; } = new();
    }

    private class GeminiAppointment
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string? Location { get; set; }
    }

    private class GeminiTask
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Priority { get; set; }
    }

    private class GeminiNote
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? NoteType { get; set; }
        public bool IsPinned { get; set; }
    }
}