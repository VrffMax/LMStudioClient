using System.Collections.Generic;
using LMStudioClient.Models;

namespace LMStudioClient.Services;

/// <summary>
/// Manages chat conversations, message history, and conversation state.
/// Provides a centralized way to handle user interactions and AI responses.
/// </summary>
public class ConversationService
{
    private readonly List<ChatMessage> _history = new();
    private int _currentSessionId;

    /// <summary>
    /// Gets the current session ID (increments with each new conversation).
    /// </summary>
    public int CurrentSessionId => _currentSessionId;

    /// <summary>
    /// Gets all messages in the current conversation history.
    /// </summary>
    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    /// <summary>
    /// Clears the entire conversation history and starts a new session.
    /// </summary>
    public void ClearHistory()
    {
        _currentSessionId++;
        _history.Clear();
    }

    /// <summary>
    /// Adds a message to the conversation history.
    /// </summary>
    /// <param name="message">The chat message to add</param>
    public void AddMessage(ChatMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.Content))
            return;

        _history.Add(message);
        Console.WriteLine($"[History] Added: {message.Role} - {TruncateContent(message.Content, 50)}");
    }

    /// <summary>
    /// Adds a user message to the conversation.
    /// </summary>
    public void AddUserMessage(string content)
    {
        var message = ChatMessage.CreateUser(content);
        AddMessage(message);
    }

    /// <summary>
    /// Adds an assistant response to the conversation.
    /// </summary>
    public void AddAssistantResponse(string content)
    {
        var message = ChatMessage.CreateAssistant(content);
        AddMessage(message);
    }

    /// <summary>
    /// Gets the last N messages from history for context window management.
    /// </summary>
    /// <param name="count">Number of recent messages to retrieve</param>
    /// <returns>List of recent messages in chronological order</returns>
    public List<ChatMessage> GetRecentMessages(int count = 5)
    {
        var limit = Math.Min(count, _history.Count);
        return _history.GetRange(_history.Count - limit, limit);
    }

    /// <summary>
    /// Gets all messages for a specific role.
    /// </summary>
    public List<ChatMessage> GetMessagesByRole(string role)
    {
        return _history.Where(m => m.Role.ToLower() == role.ToLower()).ToList();
    }

    /// <summary>
    /// Checks if the conversation has reached the maximum allowed messages.
    /// </summary>
    public bool IsOverMaxHistory(int maxMessages = 10) => _history.Count >= maxMessages;

    /// <summary>
    /// Returns a copy of the entire message history.
    /// </summary>
    public List<ChatMessage> GetAllMessages() => new(_history);

    /// <summary>
    /// Truncates content for display purposes (e.g., in console output).
    /// </summary>
    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content)) return "";

        if (content.Length <= maxLength) return content;

        // Find a good place to truncate (preferably at word boundaries)
        var endIdx = Math.Min(maxLength, content.Length - 3);

        var lastSpace = content.LastIndexOf(' ', endIdx);
        var truncationPoint = lastSpace > 0 ? lastSpace : endIdx;

        return $"{content.Substring(0, truncationPoint)}...";
    }

    /// <summary>
    /// Resets the conversation to a fresh state.
    /// </summary>
    public void Reset() => ClearHistory();

    /// <summary>
    /// Gets the role of the last message in history.
    /// </summary>
    public string? GetLastRole() => _history.Count > 0 ? _history.Last().Role : null;
}
