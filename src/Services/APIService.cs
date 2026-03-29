using System.Net.Http;
using LMStudioClient.Models;
using LMStudioClient.Services;

namespace LMStudioClient.Services;

/// <summary>
/// Handles all HTTP communication with the LM Studio API.
/// Provides methods for chat completions, model listing, and other API operations.
/// </summary>
public class APIService
{
    private readonly HttpClient _httpClient;

    public Uri BaseAddress => _httpClient.BaseAddress;

    /// <summary>
    /// Initializes a new instance of the APIService with an HTTP client.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client for API requests</param>
    public APIService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Sends a chat completion request to the LM Studio API.
    /// </summary>
    /// <param name="request">The chat completion request</param>
    /// <returns>The HTTP response containing the AI's response</returns>
    public async Task<HttpResponseMessage> SendChatRequestAsync(ChatCompletionRequest request)
    {
        var jsonContent = await JsonSerializationService.CreateJsonContentAsync(request);

        var url = $"{BaseAddress}v1/chat/completions";

        var response = await _httpClient.PostAsync(url, jsonContent);
        return response;
    }

    /// <summary>
    /// Sends a streaming chat completion request to the LM Studio API.
    /// </summary>
    /// <param name="request">The chat completion request</param>
    /// <returns>The HTTP response with SSE stream for real-time token output</returns>
    public async Task<HttpResponseMessage> SendStreamingChatRequestAsync(ChatCompletionRequest request)
    {
        var jsonContent = await JsonSerializationService.CreateJsonContentAsync(request);

        var url = $"{BaseAddress}v1/chat/completions?stream=true";

        var response = await _httpClient.PostAsync(url, jsonContent);
        return response;
    }
}
