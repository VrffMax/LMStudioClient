using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SevenZipLib.NET;
using LMStudioClient.Services;

namespace LMStudioClient.LibRuProcessor;

/// <summary>
/// Service for processing .7z archives containing text files.
/// Extracts text files, reads 4096 bytes from each, processes with LMStudio API,
/// and writes responses to output directory with 'lms' suffix.
/// </summary>
public class LibRuProcessorService
{
    private readonly APIService _apiService;
    private readonly string _outputDirectory;

    public LibRuProcessorService(APIService apiService)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "processed");
        Directory.CreateDirectory(_outputDirectory);
    }

    /// <summary>
    /// Process a single .7z archive file.
    /// </summary>
    public async Task<ProcessResult> ProcessArchiveAsync(string archivePath, string outputDir = null)
    {
        var result = new ProcessResult();

        try
        {
            Console.WriteLine($"[INFO] Processing archive: {archivePath}");

            // Ensure output directory exists
            if (outputDir != null && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            await ExtractAndProcessTxtFilesAsync(archivePath, outputDir);

            result.Success = true;
            result.Message = $"Successfully processed {result.ProcessedCount} text files from {Path.GetFileName(archivePath)}";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to process archive: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Extract text files from archive and process each one with LMStudio.
    /// </summary>
    private async Task ExtractAndProcessTxtFilesAsync(string archivePath, string outputDir)
    {
        // Create temporary directory for extraction
        var tempDir = Path.Combine(Path.GetTempPath(), $"LibRuExtract_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            using var input = new BufferedStream(File.OpenRead(archivePath));
            using var stream = new MemoryStream();

            // Extract all files from archive
            IArchiveDatabaseFactory factory = SevenZipLoader.CreateDecoderFactory(input, stream);
            IArchive archive = factory.GetArchive();

            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory && entry.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessSingleTxtFileAsync(entry, tempDir, outputDir);
                }
            }

            // Cleanup temporary directory
            Directory.Delete(tempDir, true);
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            Console.Error.WriteLine($"[WARNING] Some files may not have been extracted: {ex.Message}");
        }
    }

    /// <summary>
    /// Process a single text file from the archive.
    /// </summary>
    private async Task ProcessSingleTxtFileAsync(IArchiveEntry entry, string tempDir, string outputDir)
    {
        try
        {
            var fileName = entry.FileName;
            var outputPath = Path.Combine(tempDir, fileName);

            // Extract file from archive
            await using (var entryStream = entry.OpenInput())
            {
                await entryStream.CopyToAsync(File.Create(outputPath));
            }

            // Read first 4096 bytes
            byte[] contentBytes = File.ReadAllBytes(outputPath);
            int bytesRead = Math.Min(4096, contentBytes.Length);
            string content = Encoding.UTF8.GetString(contentBytes, 0, bytesRead).TrimEnd('\n', '\r');

            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine($"[WARNING] Empty file skipped: {fileName}");
                return;
            }

            // Process with LMStudio
            var response = await ProcessWithLmStudioAsync(content, fileName);

            // Write result to output directory
            string originalNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string outputFile = Path.Combine(outputDir, $"{originalNameWithoutExt}.txt.lms");

            await File.WriteAllTextAsync(outputFile, response, Encoding.UTF8);

            Console.WriteLine($"[SUCCESS] Processed: {fileName} -> {Path.GetFileName(outputFile)}");
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            Console.Error.WriteLine($"[WARNING] Failed to process file {entry.FileName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Send content to LMStudio for processing.
    /// </summary>
    private async Task<string> ProcessWithLmStudioAsync(string content, string sourceFile)
    {
        var request = new ChatCompletionRequest
        {
            Model = System.Environment.GetEnvironmentVariable("LMSTUDIO_MODEL") ?? "default",
            Prompt = $"""
                You are a document processing assistant. Analyze the following text snippet and provide a concise summary or analysis.

                Original file: {sourceFile}

                Content (first 4096 bytes):
                ---BEGIN_CONTENT---
                {content}
                ---END_CONTENT---

                Please process this content appropriately for the context of document analysis.
                """,
            Temperature = double.Parse(System.Environment.GetEnvironmentVariable("LMSTUDIO_TEMPERATURE") ?? "0.7"),
            MaxTokens = int.Parse(System.Environment.GetEnvironmentVariable("LMSTUDIO_MAX_TOKENS") ?? "512")
        };

        var response = await _apiService.SendChatRequestAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"LMStudio API error: {(int)response.StatusCode}");
        }

        var jsonContent = await response.Content.ReadAsStringAsync();
        return JsonSerializationService.ExtractMessageContent(jsonContent) ?? "Processing failed";
    }
}

/// <summary>
/// Result of archive processing operation.
/// </summary>
public class ProcessResult
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ProcessedCount { get; set; }
}
