using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LMStudioClient.Models;
using LMStudioClient.Services;

namespace LMStudioClient;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== LMStudioClient v1.0.0 ===");
        Console.WriteLine("Local LLM Chat Interface for .NET 8\n");

        var config = ConfigService.ParseArguments(args);
        ConfigService.PrintHelp();

        if (config.ListModels)
            await ListAvailableModels(config.BaseUrl);
        else
            await RunChatSession(config);

        Console.WriteLine("\nGoodbye!");
    }

    private static async Task RunChatSession(LMStudioConfig config)
    {
        var conversation = new ConversationService();

        if (!string.IsNullOrEmpty(config.SystemPrompt))
            conversation.AddMessage(ChatMessage.CreateSystem(config.SystemPrompt));

        Console.WriteLine($"\nModel: {config.ModelName ?? "auto"}");
        Console.WriteLine($"Temperature: {config.Temperature} | Max Tokens: {config.MaxTokens}\n");
        Console.WriteLine("Type 'exit' to quit, or press Enter for a new chat session.\n");
        Console.WriteLine("----------------------------------------");

        while (true)
        {
            var userPrompt = ConsoleInputService.ReadLine("You: ");

            if (string.IsNullOrEmpty(userPrompt))
                continue;

            if (userPrompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            conversation.AddUserMessage(userPrompt);

            var request = BuildChatRequest(config, conversation.History);

            Console.Write("Assistant: ");
            Console.CursorVisible = false;

            try
            {
                using var httpClient = new HttpClientService().CreateClient(config.BaseUrl, 60);
                var apiService = new APIService(httpClient);

                if (config.UseStreaming)
                    await ProcessStreamingResponse(apiService, request, userPrompt);
                else
                    await ProcessStandardResponse(apiService, request, userPrompt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nError: {ex.Message}");
            }
            finally
            {
                Console.CursorVisible = true;
                Console.WriteLine();
            }

            var lastMessage = conversation.History.LastOrDefault();
            if (lastMessage?.Role == "assistant" && !string.IsNullOrEmpty(lastMessage.Content))
            {
                conversation.AddAssistantResponse(lastMessage.Content);
            }
        }
    }

    private static async Task ProcessStreamingResponse(APIService apiService, ChatCompletionRequest request, string userPrompt)
    {
        try
        {
            Console.WriteLine($"[DEBUG] Sending streaming request to {apiService.BaseAddress}");
            using var response = await apiService.SendStreamingChatRequestAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Console.CursorVisible = false;

                try
                {
                    using var stream = await response.Content.ReadAsStreamAsync();

                    var service = new StreamingService();
                    var result = await service.ProcessStreamAsync(stream);

                    if (string.IsNullOrEmpty(result))
                        Console.WriteLine("[WARNING] Empty streaming response from LM Studio");
                    else
                        Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Streaming Error]: {ex.Message}");
                }
                finally
                {
                    Console.CursorVisible = true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Chat Error]: {ex.Message}");
        }
    }

    private static async Task ProcessStandardResponse(APIService apiService, ChatCompletionRequest request, string userPrompt)
    {
        try
        {
            Console.WriteLine($"[DEBUG] Sending standard request to {apiService.BaseAddress}");
            using var response = await apiService.SendChatRequestAsync(request);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // Read as string for non-streaming responses
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"[DEBUG] Received JSON ({jsonContent.Length} bytes)...");

                    if (string.IsNullOrEmpty(jsonContent))
                    {
                        Console.WriteLine("[WARNING] Empty response from LM Studio");
                        return;
                    }

                    // Extract content using JsonSerializationService
                    var content = JsonSerializationService.ExtractMessageContent(jsonContent);

                    if (string.IsNullOrEmpty(content))
                    {
                        Console.WriteLine("\n[DEBUG] Raw JSON: " + jsonContent.Substring(0, Math.Min(200, jsonContent.Length)));
                        Console.WriteLine("[WARNING] Could not extract message from response");
                        return;
                    }

                    Console.WriteLine($"Assistant: {content}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Error]: {ex.Message}");
                    if (Environment.GetEnvironmentVariable("DEBUG") == "1")
                        Console.Error.WriteLine(ex.StackTrace);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Error]: {ex.Message}");
        }
    }

    private static async Task ListAvailableModels(string baseUrl)
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        try
        {
            Console.Write("\nConnecting to ");
            await ConsoleInputService.DisplayLoadingAsync(baseUrl, 5000);

            using var response = await client.GetAsync($"{baseUrl}/v1/models");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to list models: {(int)response.StatusCode}");

            Console.WriteLine($"\nAvailable Models on {baseUrl}:");
            Console.WriteLine(new string('=', 60));

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("data", out var data))
                foreach (var model in data.EnumerateArray())
                    if (model.TryGetProperty("id", out var idProp))
                        Console.WriteLine($"  ✓ {idProp.GetString() ?? "Unknown"}");

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nError: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }

    private static ChatCompletionRequest BuildChatRequest(LMStudioConfig config, IReadOnlyList<ChatMessage> history)
    {
        var messages = new List<LMStudioClient.Models.ChatMessage>(history);

        return new LMStudioClient.Models.ChatCompletionRequest(
            model: config.ModelName ?? "auto",
            messages: messages,
            temperature: config.Temperature,
            maxTokens: config.MaxTokens,
            stream: config.UseStreaming);
    }
}
