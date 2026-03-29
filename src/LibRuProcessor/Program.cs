using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LMStudioClient.LibRuProcessor;
using LMStudioClient.Services;

namespace LMStudioClient.LibRuProcessor;

/// <summary>
/// Main entry point for the LibRuProcessor application.
/// Scans .7z archives, processes text files with LMStudio API, and writes results.
/// </summary>
public class Program
{
    private static readonly string Version = "1.0.0";

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"=== LibRuProcessor v{Version} ===");

        var options = ParseArguments(args);

        if (options == null || !options.Input.HasValue())
        {
            ShowUsage();
            return 1;
        }

        try
        {
            var apiService = CreateAPIService(options.Url, options.Model);
            var batchProcessor = new LibRuBatchProcessor(apiService);

            if (File.Exists(options.Value))
            {
                // Process single archive file
                batchProcessor.AddArchivePath(options.Value);
            }
            else if (Directory.Exists(options.Value))
            {
                // Process all .7z files in directory
                batchProcessor.AddArchivesFromDirectory(options.Value);
            }
            else
            {
                Console.Error.WriteLine($"[ERROR] Path not found: {options.Value}");
                return 1;
            }

            if (options.DryRun)
            {
                ShowDryRunInfo(batchProcessor);
                return 0;
            }

            // Process based on mode
            var batchResult = options.Parallel && !options.Stream
                ? await batchProcessor.ProcessAllParallelAsync(options.MaxConcurrency)
                : await batchProcessor.ProcessAllAsync();

            PrintBatchSummary(batchResult);

            return batchResult.SuccessRate == 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FATAL ERROR] {ex.Message}");
            if (options.Verbose)
                Console.Error.WriteLine(ex.StackTrace);
            return 2;
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine(@"LibRuProcessor - Process .7z archives with LMStudio API

Usage: dotnet run --project LibRuProcessor.csproj [options]

Options:
  --input <path>          Input directory or single .7z archive (required)
  --output <path>         Output directory for processed files (default: ./processed/)
  --url <string>          LMStudio API URL (default: from environment or localhost:1234/v1)
  --model <name>          Specific model to use
  --temperature <float>   Temperature 0-2.0 (default: 0.7)
  --max-tokens <int>      Maximum response tokens (default: 512, max: 4096)
  --stream                Enable streaming mode for LMStudio requests
  --verbose               Show detailed progress information
  --dry-run               List files to process without executing
  --parallel              Process archives in parallel mode
  --conc <int>            Maximum concurrent operations (default: 4, only with --parallel)

Examples:
  # Process all .7z archives in current directory
  dotnet run --project LibRuProcessor.csproj -- --input ./archives/

  # Process specific archive with custom settings
  dotnet run --project LibRuProcessor.csproj -- --input document.7z --model llama-3 --temperature 0.5

  # Dry-run to see what would be processed
  dotnet run --project LibRuProcessor.csproj -- --input ./archives/ --dry-run");
    }

    private static void ShowDryRunInfo(LibRuBatchProcessor processor)
    {
        Console.WriteLine($"\n[Dry Run] Found {processor.ArchiveCount} archive(s):");

        foreach (var path in processor._archivePaths)
        {
            var dirName = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            Console.WriteLine($"  - [{dirName}] {fileName}");
        }

        Console.WriteLine("\nProcessed files will be written to: ./processed/");
    }

    private static void PrintBatchSummary(BatchResult result)
    {
        Console.WriteLine($"\n=== Processing Summary ===");
        Console.WriteLine($"Total Archives:     {result.TotalArchives}");
        Console.WriteLine($"Successfully Processed: {result.ProcessedArchives}");
        Console.WriteLine($"Failed Archives:      {result.FailedArchives}");
        Console.WriteLine($"Success Rate:         {(result.SuccessRate * 100).ToString("F2")}%");

        if (result.Results.Any(r => !r.Success))
        {
            Console.Error.WriteLine($"\n[WARNING] The following archives failed:");
            foreach (var r in result.Results.Where(x => !x.Success))
            {
                Console.Error.WriteLine($"  - {Path.GetFileName(r.ArchivePath)}: {r.ErrorMessage ?? "Unknown error"}");
            }
        }

        var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "processed");
        if (Directory.Exists(outputDir) && Directory.GetFiles(outputDir).Any())
        {
            Console.WriteLine($"\nOutput files written to: {outputDir}");
            Console.WriteLine($"Total output files: {Directory.GetFiles(outputDir).Length}");
        }

        Console.WriteLine();
    }

    private static void ShowUsage() => Console.WriteLine(@"LibRuProcessor v" + Version);

    public static CommandLineOptions? ParseArguments(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--input":
                    if (i + 1 >= args.Length) return null;
                    options.Input = args[++i];
                    break;

                case "--output":
                    if (i + 1 >= args.Length) return null;
                    options.Output = args[++i];
                    break;

                case "--url":
                    if (i + 1 >= args.Length) return null;
                    options.Url = args[++i];
                    break;

                case "--model":
                    if (i + 1 >= args.Length) return null;
                    options.Model = args[++i];
                    break;

                case "--temperature":
                    if (i + 1 >= args.Length) return null;
                    try
                    {
                        var temp = double.Parse(args[++i]);
                        options.Temperature = Math.Clamp(temp, 0.0f, 2.0f);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"[ERROR] Invalid temperature value: {args[i]}");
                        return null;
                    }
                    break;

                case "--max-tokens":
                    if (i + 1 >= args.Length) return null;
                    try
                    {
                        var tokens = int.Parse(args[++i]);
                        options.MaxTokens = Math.Clamp(tokens, 32, 4096);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"[ERROR] Invalid max-tokens value: {args[i]}");
                        return null;
                    }
                    break;

                case "--stream":
                    options.Stream = true;
                    break;

                case "--verbose":
                    options.Verbose = true;
                    break;

                case "--dry-run":
                    options.DryRun = true;
                    break;

                case "--parallel":
                    options.Parallel = true;
                    break;

                case "--conc" or "--concurrency":
                    if (i + 1 >= args.Length) return null;
                    try
                    {
                        var conc = int.Parse(args[++i]);
                        options.MaxConcurrency = Math.Clamp(conc, 1, 8);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"[ERROR] Invalid concurrency value: {args[i]}");
                        return null;
                    }
                    break;

                case "--help" or "-h":
                    ShowUsage();
                    Environment.Exit(0);
                    break;
            }
        }

        if (options.Input == null)
            options.Input = "."; // Default to current directory

        return options ?? new CommandLineOptions { Input = "" };
    }

    private static APIService CreateAPIService(string? url, string? model)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = string.IsNullOrEmpty(url)
                ? (new Uri(System.Environment.GetEnvironmentVariable("LMSTUDIO_URL") ?? "http://localhost:1234/v1"))
                : new Uri(url.TrimEnd('/'))
        };

        return new APIService(httpClient);
    }

    private class CommandLineOptions
    {
        public string? Input { get; set; }
        public string? Output { get; set; }
        public string? Url { get; set; }
        public string? Model { get; set; } = System.Environment.GetEnvironmentVariable("LMSTUDIO_MODEL");
        public float Temperature { get; set; } = double.Parse(System.Environment.GetEnvironmentVariable("LMSTUDIO_TEMPERATURE") ?? "0.7");
        public int MaxTokens { get; set; } = int.Parse(System.Environment.GetEnvironmentVariable("LMSTUDIO_MAX_TOKENS") ?? "512");
        public bool Stream { get; set; }
        public bool Verbose { get; set; }
        public bool DryRun { get; set; }
        public bool Parallel { get; set; }
        public int MaxConcurrency { get; set; } = 4;

        public string Value => Input ?? "";
        public bool HasValue() => !string.IsNullOrEmpty(Input);
    }
}
