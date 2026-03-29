using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using LMStudioClient.Models;

namespace LMStudioClient.Services;

/// <summary>
/// Handles Server-Sent Events (SSE) streaming responses from the LM Studio API.
/// Provides real-time token-by-token output for chat completions.
/// </summary>
public class StreamingService
{
    /// <summary>
    /// Processes an SSE stream and extracts token deltas for real-time display.
    /// </summary>
    public async Task<string> ProcessStreamAsync(Stream stream)
    {
        var completeResponse = new StringBuilder();

        try
        {
            using var reader = new StreamReader(stream);
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var dataContent = line.Substring(6).Trim();

                if (!string.IsNullOrEmpty(dataContent))
                {
                    try
                    {
                        using var jsonDocument = JsonDocument.Parse(dataContent);

                        foreach (var choice in jsonDocument.RootElement.EnumerateArray())
                        {
                            if (choice.TryGetProperty("delta", out var delta))
                            {
                                if (delta.TryGetProperty("content", out var contentProp))
                                {
                                    var token = contentProp.GetString();

                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        completeResponse.Append(token);
                                    }
                                }

                                if (choice.TryGetProperty("message", out var message) &&
                                    message.TryGetProperty("content", out var messageContent))
                                {
                                    var content = messageContent.GetString();

                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        completeResponse.Append(content);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Streaming] Error parsing JSON: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            stream.Dispose();
        }

        return completeResponse.ToString();
    }

    /// <summary>
    /// Processes an SSE stream with callback for real-time token display.
    /// </summary>
    public async Task ProcessStreamAsync(Stream stream, Action<string> onTokenReceived)
    {
        try
        {
            using var reader = new StreamReader(stream);
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var dataContent = line.Substring(6).Trim();

                if (!string.IsNullOrEmpty(dataContent))
                {
                    try
                    {
                        using var jsonDocument = JsonDocument.Parse(dataContent);

                        foreach (var choice in jsonDocument.RootElement.EnumerateArray())
                        {
                            if (choice.TryGetProperty("delta", out var delta))
                            {
                                if (delta.TryGetProperty("content", out var contentProp))
                                {
                                    var token = contentProp.GetString();

                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        onTokenReceived(token);
                                    }
                                }

                                if (choice.TryGetProperty("message", out var message) &&
                                    message.TryGetProperty("content", out var messageContent))
                                {
                                    var content = messageContent.GetString();

                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        onTokenReceived(content);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Streaming] Error: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            stream.Dispose();
        }
    }

    /// <summary>
    /// Validates an SSE data line format.
    /// </summary>
    public static bool IsValidSseLine(string? line)
    {
        if (line == null) return false;
        return !string.IsNullOrWhiteSpace(line) && line.StartsWith("data: ");
    }

    /// <summary>
    /// Extracts the JSON payload from an SSE data line.
    /// </summary>
    public static string? ExtractSsePayload(string? line)
    {
        if (line == null) return null;
        return IsValidSseLine(line) ? line.Substring(6).Trim() : null;
    }

    /// <summary>
    /// Creates a chat completion request specifically for streaming.
    /// </summary>
    public static ChatCompletionRequest CreateStreamingRequest(IList<ChatMessage>? messages = null, double temperature = 0.7, int maxTokens = 512)
    {
        var messageList = messages ?? new List<ChatMessage>();
        return new LMStudioClient.Models.ChatCompletionRequest(
            model: "",
            messages: messageList,
            temperature: Math.Clamp(temperature, 0.0, 2.0),
            maxTokens: Math.Max(1, maxTokens),
            stream: true);
    }
}
