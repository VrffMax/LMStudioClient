using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LMStudioClient.LibRuProcessor;

/// <summary>
/// Batch processor for handling multiple .7z archives.
/// Supports both sequential and parallel processing modes with configurable concurrency.
/// </summary>
public class LibRuBatchProcessor : LibRuProcessorService
{
    private readonly List<string> _archivePaths = new();
    private int _processedCount = 0;
    private int _totalCount = 0;

    public LibRuBatchProcessor(APIService apiService) : base(apiService)
    {
    }

    /// <summary>
    /// Add an archive path to the processing queue.
    /// </summary>
    public void AddArchivePath(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Archive not found: {path}");

        _archivePaths.Add(Path.GetFullPath(path));
        _totalCount++;
    }

    /// <summary>
    /// Load all .7z archives from a directory.
    /// </summary>
    public void AddArchivesFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var sevenZipFiles = Directory.GetFiles(directoryPath, "*.7z", SearchOption.TopDirectoryOnly);

        foreach (var file in sevenZipFiles)
        {
            AddArchivePath(file);
        }
    }

    /// <summary>
    /// Process all archives sequentially.
    /// </summary>
    public async Task<BatchResult> ProcessAllAsync()
    {
        var results = new List<ProcessResult>(_totalCount);

        try
        {
            Console.WriteLine($"[INFO] Processing {_totalCount} archive(s) sequentially...");

            foreach (var path in _archivePaths)
            {
                results.Add(await ProcessArchiveAsync(path));
                _processedCount++;

                double progress = (_processedCount * 100.0) / _totalCount;
                Console.WriteLine($"[INFO] Progress: {progress:F1}%");

                if (!results.Last().Success && !System.Environment.GetEnvironmentVariable("CONTINUE_ON_ERROR")?.ToLower() == "true")
                {
                    break; // Stop on first error unless CONTINUE_ON_ERROR is set
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Batch processing failed: {ex.Message}");
            throw;
        }

        var successCount = results.Count(r => r.Success);

        return new BatchResult
        {
            TotalArchives = _totalCount,
            ProcessedArchives = successCount,
            FailedArchives = _totalCount - successCount,
            Results = results,
            SuccessRate = _totalCount > 0 ? (double)successCount / _totalCount : 0
        };
    }

    /// <summary>
    /// Process all archives with parallel execution.
    /// </summary>
    public async Task<BatchResult> ProcessAllParallelAsync(int maxConcurrency = 4)
    {
        var results = new List<Task<ProcessResult>>();

        try
        {
            Console.WriteLine($"[INFO] Processing {_totalCount} archive(s) in parallel with max concurrency: {maxConcurrency}...");

            foreach (var path in _archivePaths)
            {
                results.Add(Task.Run(() => ProcessArchiveAsync(path)));

                // Limit concurrent operations
                if (results.Count >= maxConcurrency)
                {
                    await Task.WhenAll(results.Take(maxConcurrency).Select(t => t));
                    results.Clear();
                }
            }

            // Wait for remaining tasks
            if (results.Count > 0)
            {
                await Task.WhenAll(results);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Parallel batch processing failed: {ex.Message}");
            throw;
        }

        var finalResults = results.Select(r => r.Result).ToList();

        int successCount = 0;
        foreach (var result in finalResults)
        {
            if (result.Success)
                successCount++;
        }

        return new BatchResult
        {
            TotalArchives = _totalCount,
            ProcessedArchives = successCount,
            FailedArchives = _totalCount - successCount,
            Results = finalResults,
            SuccessRate = _totalCount > 0 ? (double)successCount / _totalCount : 0
        };
    }

    /// <summary>
    /// Get the count of archives to process.
    /// </summary>
    public int ArchiveCount => _archivePaths.Count;

    /// <summary>
    /// Clear all queued archives.
    /// </summary>
    public void Clear()
    {
        _archivePaths.Clear();
        _processedCount = 0;
        _totalCount = 0;
    }
}

/// <summary>
/// Result summary of batch processing operation.
/// </summary>
public class BatchResult
{
    /// <summary>Total number of archives in the queue.</summary>
    public int TotalArchives { get; set; }

    /// <summary>Number of successfully processed archives.</summary>
    public int ProcessedArchives { get; set; }

    /// <summary>Number of failed archives.</summary>
    public int FailedArchives { get; set; }

    /// <summary>List of individual processing results.</summary>
    public List<ProcessResult> Results { get; set; } = new();

    /// <summary>Success rate as a decimal (0.0 to 1.0).</summary>
    public double SuccessRate => TotalArchives > 0 ? ProcessedArchives / (double)TotalArchives : 0;
}

/// <summary>
/// Result of processing a single archive.
/// </summary>
public class ArchiveResult
{
    /// <summary>Name of the processed archive.</summary>
    public string ArchiveName { get; set; } = string.Empty;

    /// <summary>Full path to the archive file.</summary>
    public string ArchivePath { get; set; } = string.Empty;

    /// <summary>List of successfully processed text files.</summary>
    public List<string> ProcessedFiles { get; set; } = new();

    /// <summary>Total number of text files found in archive.</summary>
    public int TotalTextFiles { get; set; }

    /// <summary>Number of text files successfully extracted and processed.</summary>
    public int SuccessfullyProcessed { get; set; }

    /// <summary>Error message if processing failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Success status of the archive processing.</summary>
    public bool Success => ErrorMessage == null && SuccessfullyProcessed > 0;
}
