using System;
using System.Collections.Generic;

namespace LMStudioClient.Models;

/// <summary>
/// Represents a message in the chat conversation.
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";

    /// <summary>
    /// Creates a new chat message with specified role and content.
    /// </summary>
    /// <param name="role">The role of the message sender (user, assistant, system)</param>
    /// <param name="content">The text content of the message</param>
    public ChatMessage(string role, string content)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Content = content ?? "";
    }

    /// <summary>
    /// Creates a system-level instruction message.
    /// </summary>
    /// <param name="content">The system prompt or instruction</param>
    public static ChatMessage CreateSystem(string content) => new("system", content);

    /// <summary>
    /// Creates a user input message.
    /// </summary>
    /// <param name="content">The user's text input</param>
    public static ChatMessage CreateUser(string content) => new("user", content);

    /// <summary>
    /// Creates an assistant response message.
    /// </summary>
    /// <param name="content">The AI-generated response</param>
    public static ChatMessage CreateAssistant(string content) => new("assistant", content);

    /// <summary>
    /// Checks if this is a system message.
    /// </summary>
    public bool IsSystem => Role == "system";

    /// <summary>
    /// Checks if this is a user message.
    /// </summary>
    public bool IsUser => Role == "user";

    /// <summary>
    /// Checks if this is an assistant message.
    /// </summary>
    public bool IsAssistant => Role == "assistant";

    /// <summary>
    /// Returns a string representation of the chat message.
    /// </summary>
    public override string ToString() => $"{Role}: {Content}";
}

/// <summary>
/// Configuration settings for the LM Studio client application.
/// </summary>
public class LMStudioConfig
{
    private const int DefaultTimeoutSeconds = 60;
    private const double DefaultTemperature = 0.7;
    private const int DefaultMaxTokens = 512;

    /// <summary>
    /// Base URL of the LM Studio API server.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:1234";

    /// <summary>
    /// Name of the model to use, or empty string for auto-selection.
    /// </summary>
    public string ModelName { get; set; } = "";

    /// <summary>
    /// System prompt instruction that guides the AI's behavior.
    /// </summary>
    public string SystemPrompt { get; set; } = "";

    /// <summary>
    /// Temperature parameter controlling response randomness (0.0-2.0).
    /// Lower values = more focused, higher values = more creative.
    /// </summary>
    public double Temperature { get; set; } = DefaultTemperature;

    /// <summary>
    /// Maximum number of tokens to generate in the response.
    /// </summary>
    public int MaxTokens { get; set; } = DefaultMaxTokens;

    /// <summary>
    /// Whether to use streaming mode for real-time token output.
    /// </summary>
    public bool UseStreaming { get; set; } = false;

    /// <summary>
    /// Whether to list available models and exit.
    /// </summary>
    public bool ListModels { get; set; } = false;

    /// <summary>
    /// Creates a new configuration with specified parameters.
    /// </summary>
    /// <param name="baseUrl">Base API URL</param>
    /// <param name="modelName">Model name to use (empty for auto-select)</param>
    public LMStudioConfig(string baseUrl = "http://localhost:1234", string? modelName = null)
    {
        BaseUrl = baseUrl;
        ModelName = modelName ?? "";
        SystemPrompt = "";
        Temperature = DefaultTemperature;
        MaxTokens = DefaultMaxTokens;
        UseStreaming = false;
        ListModels = false;

        // Validate configuration
        Validate();
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    private void Validate()
    {
        if (string.IsNullOrEmpty(BaseUrl))
            throw new ArgumentException("Base URL is required", nameof(BaseUrl));

        try
        {
            // Optional: Add more validation here as needed
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Configuration error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates temperature to a valid range.
    /// </summary>
    public void ValidateTemperature() => Temperature = Math.Clamp(Temperature, 0.0, 2.0);

    /// <summary>
    /// Updates max tokens to a reasonable limit.
    /// </summary>
    public void ValidateMaxTokens(int? maxLimit = null) => MaxTokens = maxLimit.HasValue
        ? Math.Min(MaxTokens, maxLimit.Value)
        : Math.Min(MaxTokens, 4096);

    /// <summary>
    /// Returns a string representation of the configuration.
    /// </summary>
    public override string ToString() => $"LMStudioConfig: {BaseUrl} | Model: {ModelName}";
}
