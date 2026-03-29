using System;
using System.Threading.Tasks;

namespace LMStudioClient.Services;

/// <summary>
/// Manages console input and output operations.
/// Provides a centralized interface for user interaction with the application.
/// </summary>
public class ConsoleInputService
{
    /// <summary>
    /// Prompts the user for input and reads their response from the console.
    /// </summary>
    /// <param name="prompt">The message to display before reading input</param>
    /// <returns>The user's input as a string, or null if cancelled (Ctrl+C)</returns>
    public static string? ReadLine(string prompt)
    {
        try
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is EndOfStreamException)
        {
            // User cancelled input - treat as null/cancellation
            return null;
        }
    }

    /// <summary>
    /// Reads a yes/no response from the user.
    /// </summary>
    public static bool ReadConfirmation(string prompt = "Continue? (y/n): ")
    {
        var input = ReadLine(prompt);

        if (string.IsNullOrEmpty(input))
            return false;

        return input.ToLowerInvariant() is "y" or "yes";
    }

    /// <summary>
    /// Reads a numeric value from the console with validation.
    /// </summary>
    public static int ReadNumber(string prompt, int defaultValue = 0)
    {
        while (true)
        {
            var input = ReadLine(prompt);

            if (!int.TryParse(input ?? default, out var number))
            {
                Console.WriteLine("Please enter a valid number.");
                continue;
            }

            return number;
        }
    }

    /// <summary>
    /// Reads a floating-point value from the console with validation.
    /// </summary>
    public static double ReadNumber(string prompt, double defaultValue = 0.0)
    {
        while (true)
        {
            var input = ReadLine(prompt);

            if (!double.TryParse(input ?? default, out var number))
            {
                Console.WriteLine("Please enter a valid number.");
                continue;
            }

            return number;
        }
    }

    /// <summary>
    /// Writes formatted information to the console.
    /// </summary>
    public static void WriteInfo(string message) => Console.WriteLine(message);

    /// <summary>
    /// Writes an error message to the console (red text).
    /// </summary>
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a warning message to the console (yellow text).
    /// </summary>
    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARNING] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a success message to the console (green text).
    /// </summary>
    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SUCCESS] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Displays a loading indicator while waiting for an operation to complete.
    /// </summary>
    public static async Task DisplayLoadingAsync(string message, int timeoutMs = 5000)
    {
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(timeoutMs))
        {
            Console.Write($"\r[{message}] ");
            await Task.Delay(100);
        }

        Console.WriteLine(); // Move to next line after timeout
    }

    /// <summary>
    /// Hides the cursor from the console.
    /// </summary>
    public static void HideCursor() => Console.CursorVisible = false;

    /// <summary>
    /// Shows the cursor in the console.
    /// </summary>
    public static void ShowCursor() => Console.CursorVisible = true;

    /// <summary>
    /// Clears the console screen.
    /// </summary>
    public static void ClearScreen() => Console.Clear();

    /// <summary>
    /// Reads a list of items separated by newlines until an empty line is entered.
    /// </summary>
    public static List<string> ReadMultiLineInput(string prompt, int maxLines = 10)
    {
        var items = new List<string>();

        while (items.Count < maxLines)
        {
            var input = ReadLine(prompt);

            if (string.IsNullOrEmpty(input))
                break;

            items.Add(input.Trim());
        }

        return items;
    }

    /// <summary>
    /// Reads a string that matches the provided pattern until validation succeeds.
    /// </summary>
    public static string ReadValidatedInput(string prompt, Func<string, bool> validator)
    {
        while (true)
        {
            var input = ReadLine(prompt);

            if (!string.IsNullOrEmpty(input) && validator.Invoke(input))
                return input;

            Console.WriteLine("Invalid input. Please try again.");
        }
    }
}
