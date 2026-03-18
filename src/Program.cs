using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LMStudioClient;

internal class Program
{
    private const int DefaultTimeoutSeconds = 60;
    private const double DefaultTemperature = 0.7;
    private const int DefaultMaxTokens = 512;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LMStudioClient v1.0.0 ===");
        Console.WriteLine("Local LLM Chat Interface for .NET 8\n");

        var config = ParseArguments(args);

        try
        {
            if (config.ListModels)
                await ListAvailableModels(config.BaseUrl, DefaultTimeoutSeconds);
            else
                await RunChatSession(config);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("connection refused"))
        {
            Console.Error.WriteLine($"\nError: Cannot connect to LM Studio server at '{config.BaseUrl}'");
            Console.Error.WriteLine("Make sure LM Studio is running on localhost port 1234.");
            Console.Error.WriteLine("\nTo start LM Studio, download from: https://lmstudio.ai\n");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nUnexpected error: {ex.Message}");
            if (Environment.GetEnvironmentVariable("DEBUG") == "1")
                Console.Error.WriteLine(ex.StackTrace);
        }
    }

    private static LMStudioConfig ParseArguments(string[] args)
    {
        var config = new LMStudioConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--model":
                    if (i + 1 < args.Length) config.ModelName = args[++i]; break;
                case "--system":
                    if (i + 1 < args.Length) config.SystemPrompt = args[++i]; break;
                case "--temperature":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out var temp))
                        config.Temperature = Math.Clamp(temp, 0.0, 2.0); break;
                case "--max-tokens":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var tokens))
                        config.MaxTokens = Math.Min(tokens, 4096); break;
                case "--stream":
                    config.UseStreaming = true; break;
                case "--list-models" or "-l":
                    config.ListModels = true; break;
                case "--help" or "-h":
                    PrintHelp(); Environment.Exit(0); break;
                case "--url":
                    if (i + 1 < args.Length) config.BaseUrl = args[++i]; break;
            }
        }

        var lmStudioUrl = Environment.GetEnvironmentVariable("LMSTUDIO_URL");
        if (!string.IsNullOrEmpty(lmStudioUrl)) config.BaseUrl = lmStudioUrl;

        return config;
    }

    private static async Task RunChatSession(LMStudioConfig config)
    {
        Console.WriteLine($"Connecting to {config.BaseUrl}...\n");

        var client = CreateHttpClient(config.BaseUrl, DefaultTimeoutSeconds);
        var history = new List<ChatMessage>();

        if (!string.IsNullOrEmpty(config.SystemPrompt))
            history.Add(new ChatMessage("system", config.SystemPrompt));

        Console.WriteLine($"Model: {config.ModelName ?? "auto"}");
        Console.WriteLine($"Temperature: {config.Temperature} | Max Tokens: {config.MaxTokens}\n");
        Console.WriteLine("----------------------------------------");

        while (true)
        {
            try
            {
                if (config.UseStreaming)
                    await RunInteractiveChatStreaming(client, history, config);
                else
                    await RunInteractiveChatStandard(client, history, config);

                // Clear line for next prompt using alternative approach
                Console.Write("\r");
                Console.Write(new string(' ', Console.BufferWidth));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nError during chat: {ex.Message}");
                break;
            }
        }
    }

    private static async Task RunInteractiveChatStreaming(HttpClient client, List<ChatMessage> history, LMStudioConfig config)
    {
        Console.Write("You: ");
        var userPrompt = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userPrompt)) return;

        if (userPrompt.ToLower().Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\nGoodbye!");
            return;
        }

        history.Add(new ChatMessage("user", userPrompt));

        var request = BuildChatRequest(config, history);

        using var response = await client.PostAsync(
            $"{config.BaseUrl}/v1/chat/completions?stream=true",
            CreateJsonContent(request));

        Console.Write("Assistant: ");
        Console.CursorVisible = false;

        try
        {
            using var reader = new StreamReader(response.Content.ReadAsStream());
            string line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var deltaContent = ExtractTokenDelta(line);
                    if (!string.IsNullOrEmpty(deltaContent)) Console.Write(deltaContent);
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.WriteLine();
        }

        history.Add(new ChatMessage("assistant", "Response received"));
    }

    private static async Task RunInteractiveChatStandard(HttpClient client, List<ChatMessage> history, LMStudioConfig config)
    {
        Console.Write("You: ");
        var userPrompt = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userPrompt)) return;

        if (userPrompt.ToLower().Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\nGoodbye!");
            return;
        }

        history.Add(new ChatMessage("user", userPrompt));

        var request = BuildChatRequest(config, history);

        using var response = await client.PostAsync(
            $"{config.BaseUrl}/v1/chat/completions",
            CreateJsonContent(request));

        string content = "";

        if (response.IsSuccessStatusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("choices", out var choices))
                    foreach (var choice in choices.EnumerateArray())
                        if (choice.TryGetProperty("message", out var messageProp) &&
                            messageProp.TryGetProperty("content", out var contentProp))
                        {
                            content = contentProp.GetString() ?? "";
                            break;
                        }

                if (string.IsNullOrEmpty(content))
                    content = "No response received";

                Console.WriteLine($"Assistant: {content}");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse server response: {ex.Message}", ex);
            }
        }
        else
        {
            throw new HttpRequestException($"API returned status code: {(int)response.StatusCode}");
        }

        history.Add(new ChatMessage("assistant", content));
    }

    private static async Task ListAvailableModels(string baseUrl, int timeoutSeconds)
    {
        var client = CreateHttpClient(baseUrl, timeoutSeconds);

        try
        {
            using var response = await client.GetAsync($"{baseUrl}/v1/models");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to list models: {(int)response.StatusCode}");

            Console.WriteLine($"\nAvailable Models on {baseUrl}:");
            Console.WriteLine(new string('=', 60));

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("data", out var data))
                foreach (var model in data.EnumerateArray())
                    if (model.TryGetProperty("id", out var idProp))
                        Console.WriteLine($"  ✓ {idProp.GetString() ?? "Unknown"}");
        }
        finally
        {
            // Release resources
        }
    }

    private static HttpClient CreateHttpClient(string baseUrl, int timeoutSeconds)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            UseCookies = false,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    private static string? ExtractTokenDelta(string sseLine)
    {
        if (!sseLine.StartsWith("data: ")) return null;
        var jsonContent = sseLine.Substring(6);

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            if (doc.RootElement.TryGetProperty("choices", out var choices))
                foreach (var choice in choices.EnumerateArray())
                    if (choice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var contentProp))
                        return contentProp.GetString();
        }
        catch { }

        return null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("LMStudioClient - Local LLM Chat Interface");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine($"  --model <name>          Use specific model (auto-select by default)");
        Console.WriteLine($"  --system \"prompt\"       Set system prompt instruction");
        Console.WriteLine($"  --temperature <0-2.0>   Response creativity (default: {DefaultTemperature})");
        Console.WriteLine($"  --max-tokens <int>      Maximum response tokens (default: {DefaultMaxTokens}, max: 4096)");
        Console.WriteLine($"  --stream                Enable streaming output (token-by-token display)");
        Console.WriteLine($"  --list-models or -l     List available models and exit");
        Console.WriteLine();
        Console.WriteLine("Environment Variables:");
        Console.WriteLine("  LMSTUDIO_URL           Override default base URL");
    }

    public class LMStudioConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:1234";
        public string ModelName { get; set; } = null!; // null means auto-select
        public string SystemPrompt { get; set; } = "";
        public double Temperature { get; set; } = DefaultTemperature;
        public int MaxTokens { get; set; } = DefaultMaxTokens;
        public bool UseStreaming { get; set; } = false;
        public bool ListModels { get; set; } = false;

        public static LMStudioConfig Create(string baseUrl = "http://localhost:1234", string? modelName = null)
        {
            return new LMStudioConfig
            {
                BaseUrl = baseUrl,
                ModelName = modelName ?? "",
                SystemPrompt = "",
                Temperature = DefaultTemperature,
                MaxTokens = DefaultMaxTokens,
                UseStreaming = false,
                ListModels = false
            };
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    private record MessageModel(string Role, string Content);

    private class ChatCompletionRequest
    {
        public string Model { get; set; } = "";
        public List<MessageModel> Messages { get; set; } = new();
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 512;
        public bool Stream { get; set; } = false;
    }

    private static System.Net.Http.StringContent CreateJsonContent(ChatCompletionRequest request)
    {
        return new System.Net.Http.StringContent(
            JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }),
            System.Text.Encoding.UTF8,
            "application/json");
    }

    private static ChatCompletionRequest BuildChatRequest(LMStudioConfig config, List<ChatMessage> history)
    {
        return new ChatCompletionRequest
        {
            Model = config.ModelName ?? "auto",
            Messages = [..history.Select(h => new MessageModel(h.Role, h.Content))],
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens,
            Stream = config.UseStreaming
        };
    }
}
