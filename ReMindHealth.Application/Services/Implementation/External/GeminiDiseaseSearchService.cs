using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReMindHealth.Application.Interfaces.IServices;

namespace ReMindHealth.Application.Services.Implementation.External;

public class GeminiDiseaseSearchService : IDiseaseSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiDiseaseSearchService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiDiseaseSearchService(
        IConfiguration configuration,
        ILogger<GeminiDiseaseSearchService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured");

        _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<DiseaseSearchResult> SearchDiseaseAsync(
        string diseaseName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching for disease: {DiseaseName}", diseaseName);

            var prompt = BuildSearchPrompt(diseaseName);
            var response = await CallGeminiApiAsync(prompt, cancellationToken);

            _logger.LogInformation("Gemini search response received");

            var result = ParseSearchResponse(response, diseaseName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disease search");

            return new DiseaseSearchResult
            {
                DiseaseName = diseaseName,
                Description = $"Fehler bei der Suche: {ex.Message}"
            };
        }
    }

    private string BuildSearchPrompt(string diseaseName)
    {
        return $@"Du bist ein medizinischer Informationsassistent. Gib KEINE medizinische Beratung, sondern nur allgemeine Informationen.

Gib Informationen über folgende Krankheit/Diagnose: {diseaseName}

Antworte NUR mit folgendem JSON-Format (auf Deutsch):

{{
  ""diseaseName"": ""{diseaseName}"",
  ""description"": ""Kurze, verständliche Beschreibung der Krankheit (2-3 Sätze)"",
  ""symptoms"": [
    ""Symptom 1"",
    ""Symptom 2"",
    ""Symptom 3""
  ],
  ""causes"": [
    ""Ursache 1"",
    ""Ursache 2""
  ],
  ""treatments"": [
    ""Behandlungsmöglichkeit 1"",
    ""Behandlungsmöglichkeit 2"",
    ""Behandlungsmöglichkeit 3""
  ],
  ""prevention"": [
    ""Vorbeugungsmaßnahme 1"",
    ""Vorbeugungsmaßnahme 2""
  ],
  ""whenToSeeDoctor"": [
    ""Wann Sie einen Arzt aufsuchen sollten 1"",
    ""Wann Sie einen Arzt aufsuchen sollten 2""
  ],
  ""additionalInfo"": ""Zusätzliche wichtige Informationen oder Hinweise""
}}

WICHTIG: 
- Alle Informationen auf Deutsch
- Keine medizinische Beratung, nur allgemeine Informationen
- Hinweis: Immer einen Arzt konsultieren bei Beschwerden
- Nur JSON ausgeben, keine zusätzlichen Texte

**Antwort (nur JSON):**";
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
                temperature = 0.3,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 4096,
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        int maxRetries = 3;
        int retryDelay = 2000; 

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _logger.LogInformation("Calling Gemini API (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseText);
                    var generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                    if (string.IsNullOrEmpty(generatedText))
                    {
                        throw new Exception("No text generated from Gemini API");
                    }

                    return generatedText;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && i < maxRetries - 1)
                {
                    _logger.LogWarning("Gemini API overloaded, retrying in {Delay}ms...", retryDelay);
                    await Task.Delay(retryDelay, cancellationToken);
                    retryDelay *= 2; // Exponential backoff: 2s, 4s, 8s
                    continue;
                }
                throw new Exception($"Gemini API error: {response.StatusCode} - {responseText}");
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying...", i + 1);
                await Task.Delay(retryDelay, cancellationToken);
                retryDelay *= 2;
            }
        }

        throw new Exception("Gemini API failed after all retry attempts");
    }

    private DiseaseSearchResult ParseSearchResponse(string responseText, string diseaseName)
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

            var result = JsonSerializer.Deserialize<DiseaseSearchResult>(jsonText, options);
            return result ?? new DiseaseSearchResult { DiseaseName = diseaseName };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response from Gemini");
            return new DiseaseSearchResult
            {
                DiseaseName = diseaseName,
                Description = "Fehler beim Parsen der Antwort"
            };
        }
    }

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
}