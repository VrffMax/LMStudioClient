using System.Net.Http;
using System.Net.Http.Headers;

namespace LMStudioClient.Services;

/// <summary>
/// Provides HttpClient instances configured for API communication.
/// </summary>
public class HttpClientService
{
    private readonly HttpClientHandler _handler = new();

    /// <summary>
    /// Creates an HttpClient instance with default timeouts.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API</param>
    /// <returns>An HttpClient instance configured for API requests</returns>
    public HttpClient CreateClient(string baseUrl, int timeoutSeconds = 60)
    {
        var client = new HttpClient(_handler);
        client.BaseAddress = new Uri(baseUrl);

        // Set default Accept header for JSON responses
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
