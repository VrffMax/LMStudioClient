using System.Collections.Generic;

namespace LMStudioClient.Models;

/// <summary>
/// Represents a chat completion request sent to the LM Studio API.
/// </summary>
public class ChatCompletionRequest
{
    /// <summary>
    /// The ID of the model to use for generating completions.
    /// Empty string means auto-select from available models.
    /// </summary>
    public string Model { get; set; } = "";

    /// <summary>
    /// List of messages that form the conversation context.
    /// Each message has a role (user, assistant, system) and content.
    /// </summary>
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// Temperature parameter controlling response randomness.
    /// Lower values (0.0-1.0) produce more focused responses.
    /// Higher values (1.0-2.0) produce more creative responses.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of tokens to generate in the response.
    /// Must be between 1 and 4096 (LM Studio limit).
    /// </summary>
    public int MaxTokens { get; set; } = 512;

    /// <summary>
    /// Whether to enable streaming mode for real-time token output.
    /// When true, tokens are returned as they're generated.
    /// </summary>
    public bool Stream { get; set; } = false;

    /// <summary>
    /// Creates a new chat completion request with specified parameters.
    /// </summary>
    /// <param name="model">The model ID to use</param>
    /// <param name="messages">List of conversation messages</param>
    /// <param name="temperature">Response temperature (0.0-2.0)</param>
    /// <param name="maxTokens">Maximum tokens to generate</param>
    /// <param name="stream">Whether to use streaming mode</param>
    public ChatCompletionRequest(
        string model = "",
        IList<ChatMessage>? messages = null,
        double temperature = 0.7,
        int maxTokens = 512,
        bool stream = false)
    {
        Model = ValidateModel(model);
        Messages = messages ?? new List<ChatMessage>();
        Temperature = Math.Clamp(temperature, 0.0, 2.0);
        MaxTokens = Math.Max(1, maxTokens);
        Stream = stream;

        // Ensure at least one message exists
        if (Messages.Count == 0)
        {
            Messages.Add(new ChatMessage("user", "Hello"));
        }
    }

    /// <summary>
    /// Validates and normalizes the model parameter.
    /// </summary>
    private static string ValidateModel(string model) => model;

    /// <summary>
    /// Creates a chat completion request for streaming responses.
    /// </summary>
    public static ChatCompletionRequest CreateStreaming(
        IList<ChatMessage> messages,
        double temperature = 0.7,
        int maxTokens = 512) => new(null!, messages, temperature, maxTokens, true);

    /// <summary>
    /// Creates a chat completion request for standard (non-streaming) responses.
    /// </summary>
    public static ChatCompletionRequest CreateStandard(
        IList<ChatMessage> messages,
        double temperature = 0.7,
        int maxTokens = 512) => new(null!, messages, temperature, maxTokens, false);

    /// <summary>
    /// Returns a string representation of the request (for debugging).
    /// </summary>
    public override string ToString() => $"ChatCompletionRequest: Model={Model}, Messages={Messages.Count}, Stream={Stream}";
}
