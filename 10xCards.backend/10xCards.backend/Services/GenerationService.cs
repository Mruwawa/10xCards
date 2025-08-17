using _10xCards.backend.Domain;
using _10xCards.backend.Infrastructure;
using MongoDB.Driver;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _10xCards.backend.Services;

public class GenerationService
{
    private readonly MongoContext _db;
    private readonly IConfiguration _config;
    private static readonly HttpClient _http = new HttpClient();

    public GenerationService(MongoContext db, IConfiguration config)
    {
        _db = db; _config = config;
    }

    public async Task<GenerationResult> GenerateAsync(string userId, string text, CancellationToken ct = default)
    {
        var apiKey = _config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("OpenAI API key not configured");
        var model = _config["OpenAI:Model"] ?? "gpt-5-nano-2025-08-07";

        // Prompt instructing JSON-only output.
        var sys = "Jesteś asystentem generującym wysokiej jakości fiszki (pytanie-odpowiedź) w języku polskim. Zwróć TYLKO poprawny JSON bez komentarzy.";
        // Uwaga: w interpolated verbatim string do podwojenia są tylko klamry dla literalu JSON.
        var userPrompt = $@"Tekst źródłowy:\n{text}\n\nWygeneruj mi z tego tekstu kilka fiszek do nauki (max 15), mają być poprawnie sformatowane, usunięte zbędne znaki oraz mają pomóc użytkownikowi nauczyć się i powtórzyć materiał. Zwróć dokładnie JSON: {{""flashcards"": [ {{ ""front"": ""..."", ""back"": ""..."" }} ] }}. Nie dodawaj komentarzy ani tekstu poza JSON.";

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var body = new
        {
            model = model,
            messages = new object[]
            {
                new { role = "system", content = sys },
                new { role = "user", content = userPrompt }
            },
            temperature = 1,
            max_completion_tokens = 5000,
            response_format = new { type = "json_object" }
        };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        string raw;
        try
        {
            using var resp = await _http.SendAsync(req, ct);
            raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI error {(int)resp.StatusCode}: {raw}");
            }
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException("Timeout contacting OpenAI");
        }

        // Minimal extraction of assistant message content (avoid binding to SDK).
        string? assistantContent = null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            assistantContent = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch
        {
            // ignored – will fallback
        }

        List<FlashcardSuggestion> suggestions = new();
        if (!string.IsNullOrWhiteSpace(assistantContent))
        {
            // Try parse JSON directly.
            string jsonText = assistantContent.Trim();
            // Model might wrap JSON in markdown fences.
            if (jsonText.StartsWith("```)")) { /* improbable */ }
            if (jsonText.StartsWith("```"))
            {
                var idx = jsonText.IndexOf('\n');
                if (idx > -1) jsonText = jsonText[(idx + 1)..];
                jsonText = jsonText.Trim().Trim('`');
            }
            try
            {
                using var parsed = JsonDocument.Parse(jsonText);
                if (parsed.RootElement.TryGetProperty("flashcards", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var fc in arr.EnumerateArray())
                    {
                        var front = fc.GetProperty("front").GetString();
                        var back = fc.GetProperty("back").GetString();
                        if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back)) continue;
                        suggestions.Add(new FlashcardSuggestion { Front = front!, Back = back! });
                    }
                }
            }
            catch
            {
                // fallback below
            }
        }

        if (suggestions.Count == 0)
        {
            // Fallback heuristic: split sentences.
            var parts = text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts.Take(5))
            {
                suggestions.Add(new FlashcardSuggestion
                {
                    Front = p.Length > 40 ? p[..40] + "?" : p + "?",
                    Back = p
                });
            }
        }

        var session = new GenerationSession
        {
            UserId = userId,
            InputTextHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32],
            ProposedCount = suggestions.Count,
            AcceptedCount = 0
        };
        await _db.GenerationSessions.InsertOneAsync(session, cancellationToken: ct);

        return new GenerationResult(session.Id, suggestions);
    }
}

public class FlashcardSuggestion
{
    public string Front { get; set; } = null!;
    public string Back { get; set; } = null!;
}

public record GenerationResult(string SessionId, IEnumerable<FlashcardSuggestion> Suggestions);
