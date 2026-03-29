using System.Net.Http;
using System.Text;
using System.Text.Json;
using LMStudioClient.Models;

namespace LMStudioClient.Services;

/// <summary>
/// Handles JSON serialization and deserialization for API requests and responses.
/// Provides UTF-8 encoding support for Unicode characters including Cyrillic text.
/// </summary>
public class JsonSerializationService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes a chat completion request to JSON StringContent with proper UTF-8 encoding.
    /// </summary>
    /// <param name="request">The chat completion request object</param>
    /// <returns>A StringContent ready for HTTP POST requests</returns>
    public static async Task<StringContent> CreateJsonContentAsync(ChatCompletionRequest request)
    {
        var json = JsonSerializer.Serialize(request, _options);

        // Use "application/json" without charset - this is the correct format for .NET 8+
        // The previous error was caused by using "application/json; charset=utf-8" which is invalid
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Parses SSE (Server-Sent Events) line and extracts token delta from JSON.
    /// </summary>
    /// <param name="sseLine">The SSE data line in format: "data: {...json...}"</param>
    /// <returns>The extracted content string or null if parsing fails</returns>
    public static string? ExtractTokenDelta(string sseLine)
    {
        if (!sseLine.StartsWith("data: ")) return null;

        var jsonContent = sseLine.Substring(6);

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);

            // Navigate to choices[0].message.content or delta.content for streaming
            foreach (var choice in doc.RootElement.EnumerateArray())
            {
                if (choice.TryGetProperty("delta", out var delta))
                {
                    return delta.GetProperty("content").GetString();
                }

                if (choice.TryGetProperty("message", out var messageProp) &&
                    messageProp.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString();
                }
            }
        }
        catch
        {
            // Ignore parsing errors - return null
        }

        return null;
    }

    /// <summary>
    /// Parses a complete JSON response and extracts the message content.
    /// </summary>
    /// <param name="json">The raw JSON response string</param>
    /// <returns>The extracted message content or empty string if not found</returns>
    public static string ExtractMessageContent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("choices", out var choices))
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("message", out var messageProp) &&
                        messageProp.TryGetProperty("content", out var contentProp))
                    {
                        return contentProp.GetString() ?? "";
                    }
                }
            }
        }
        catch
        {
            // Return empty string on parsing errors
        }

        return "";
    }

    /// <summary>
    /// Parses a JSON response and extracts the model ID.
    /// </summary>
    /// <param name="json">The raw JSON response string</param>
    /// <returns>The extracted model ID or "Unknown" if not found</returns>
    public static string ExtractModelId(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var model in data.EnumerateArray())
                {
                    if (model.TryGetProperty("id", out var idProp))
                    {
                        return idProp.GetString() ?? "Unknown";
                    }
                }
            }
        }
        catch
        {
            // Return "Unknown" on parsing errors
        }

        return "Unknown";
    }

    /// <summary>
    /// Extracts all model IDs from a JSON response listing available models.
    /// </summary>
    public static List<string> ExtractModelIds(string json) => new()
    {
        ExtractModelId(json)
    };
}
