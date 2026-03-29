using System;
using LMStudioClient.Models;

namespace LMStudioClient.Services;

/// <summary>
/// Manages application configuration and command-line argument parsing.
/// Provides a centralized configuration system for the LM Studio client.
/// </summary>
public class ConfigService
{
    private const int DefaultTimeoutSeconds = 60;
    private const double DefaultTemperature = 0.7;
    private const int DefaultMaxTokens = 512;

    /// <summary>
    /// Parses command-line arguments and returns a configured LMStudioConfig instance.
    /// </summary>
    /// <param name="args">Command-line arguments array</param>
    /// <returns>A fully configured LMStudioConfig object</returns>
    public static LMStudioConfig ParseArguments(string[] args)
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

        // Check for environment variable override first
        var lmStudioUrl = Environment.GetEnvironmentVariable("LMSTUDIO_URL");
        if (!string.IsNullOrEmpty(lmStudioUrl))
            config.BaseUrl = lmStudioUrl;

        // If still not set, use default LM Studio URL
        if (string.IsNullOrEmpty(config.BaseUrl))
            config.BaseUrl = "http://localhost:1234";

        return config;
    }

    /// <summary>
    /// Prints help information to the console.
    /// </summary>
    public static void PrintHelp()
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

    /// <summary>
    /// Creates a new configuration with specified parameters.
    /// </summary>
    /// <param name="baseUrl">Base API URL</param>
    /// <param name="modelName">Model name to use (null for auto-select)</param>
    /// <returns>A configured LMStudioConfig instance</returns>
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

    /// <summary>
    /// Validates the current configuration for required fields.
    /// </summary>
    /// <param name="config">The configuration to validate</param>
    /// <returns>True if valid, false otherwise with error message</returns>
    public static string? ValidateConfig(LMStudioConfig config)
    {
        if (string.IsNullOrEmpty(config.BaseUrl))
            return "Base URL is required";

        try
        {
            new Uri(config.BaseUrl);
        }
        catch (UriFormatException ex)
        {
            return $"Invalid Base URL format: {ex.Message}";
        }

        if (!string.IsNullOrWhiteSpace(config.ModelName))
        {
            // Optional: Add model name validation here
        }

        return null; // Valid configuration
    }
}
