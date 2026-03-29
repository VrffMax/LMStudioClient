# LibRu Processor - Specification Document

## Overview
LibRuProcessor is a console application that scans `.7z` archive files, extracts all `.txt` files from the archive, reads 4096 bytes from each text file, sends this content to LMStudio API for processing, and writes responses with "lms" suffix back to separate output directories.

## Features
1. Scan a specified directory or archive file for `.7z` archives
2. Extract all `.txt` files from each archive
3. Read exactly 4096 bytes (or less if file is smaller) from each text file starting at the beginning
4. Send content to LMStudio API for chat completion
5. Write processed responses to output directory with "lms" suffix in filename
6. Progress tracking and error handling
7. Support for streaming or non-streaming mode

## Architecture

### Project Structure
```
LMStudioClient/
├── src/
│   ├── LibRuProcessor/
│   │   ├── LibRuProcessor.csproj      # Project file
│   │   └── LibRuProcessorService.cs   # Main processing logic
│   └── ... (existing code)
```

### Directory Layout for Processing
```
Input Directory:
├── archive1.7z           # Input archives to process
├── archive2.7z
└── ...

Output Directory:
└── processed/            # Contains .txt.lms files
    ├── document1.txt.lms
    ├── report2024.txt.lms
    └── notes.txt.lms
```

## API Configuration

### LMStudio Connection
- **Base URL**: Configurable via environment variable `LMSTUDIO_URL` or command-line argument
- **Default Model**: Auto-select first available model or configurable via `--model` flag
- **Temperature**: Default 0.7 (configurable)
- **Max Tokens**: Default 512 (configurable, max 4096)

## Processing Workflow

### Step-by-Step Flow:
1. **Input Validation**
   - Check if input path exists and is accessible
   - Validate `.7z` file integrity
   
2. **Archive Extraction**
   - Extract all `.txt` files from archive to temporary location
   - Preserve original filenames (without `.txt.lms` suffix)

3. **Content Reading**
   - Read first 4096 bytes of each text file
   - Handle encoding (UTF-8 with BOM support)
   - Skip empty or binary files

4. **LMStudio Processing**
   - Send content to `/v1/chat/completions` endpoint
   - Use system prompt: "Process this document snippet for analysis"
   - Apply configured parameters

5. **Output Writing**
   - Write response to `output_dir/filename.txt.lms`
   - Include metadata header with original filename and processing timestamp
   - Preserve original content in comments if needed

6. **Cleanup**
   - Remove temporary extraction files after successful processing
   - Report any failed items but continue processing remaining files

## Command-Line Interface

### Usage:
```bash
dotnet run --project LibRuProcessor.csproj -- [options]

Options:
  --input <path>              Input directory or .7z archive file (required)
  --output <path>             Output directory for processed files (default: ./processed/)
  --url <string>              LMStudio API URL (default: from environment or http://localhost:1234/v1)
  --model <name>              Specific model to use
  --temperature <float>       Temperature 0-2.0 (default: 0.7)
  --max-tokens <int>          Maximum response tokens (default: 512, max 4096)
  --stream                    Enable streaming output mode
  --verbose                   Show detailed progress information
  --dry-run                   List files to process without executing
```

### Examples:
```bash
# Process all .7z archives in current directory
dotnet run --project LibRuProcessor.csproj -- --input ./archives/

# Process specific archive with custom settings
dotnet run --project LibRuProcessor.csproj -- --input document.7z --model "llama-3" --temperature 0.5
```

## Error Handling

### Expected Errors:
1. **Archive not found**: Exit with code 1, show helpful message
2. **Invalid archive format**: Log warning, skip file, continue processing
3. **LMStudio connection failed**: Retry up to 3 times, then fail gracefully
4. **File too large**: Warn user that only first 4096 bytes are processed
5. **API rate limiting**: Implement exponential backoff retry strategy

### Output:
- Successful operations: Silent (unless --verbose)
- Warnings: Shown with `[WARNING]` prefix
- Errors: Shown with `[ERROR]` prefix, exit code non-zero for fatal errors
- Progress: Show percentage and file count processed

## Dependencies

### External Libraries:
1. **7zip.net** or **SharpZipLib** - For .7z archive handling
2. **System.IO.Compression** - Built-in compression support
3. **LMStudioClient.Services** - Reuse existing API service layer

### Configuration Files:
- `.env` file for storing sensitive configuration (API keys, default URLs)
- `config.json` for persistent settings

## Performance Considerations

1. **Batch Processing**: Process multiple archives in parallel if configured
2. **Memory Management**: Stream content reading to avoid memory spikes
3. **Throttling**: Respect API rate limits with configurable delays
4. **Progress Reporting**: Update console UI without blocking operations

## Security

1. Never store or log API tokens in plain text
2. Validate all input paths to prevent path traversal attacks
3. Sanitize filenames before writing output files
4. Use temporary directories with proper cleanup on failures

## Testing

### Test Cases:
1. Empty archive (no .txt files)
2. Archive with single large file (> 4096 bytes)
3. Archive with multiple small files (< 4096 bytes each)
4. Mixed content types (should skip non-.txt files)
5. Invalid archive format
6. Network failures during processing
7. LMStudio API rate limiting

### Output Files:
Each processed file should contain:
```
# Processed by LibRuProcessor
# Original: filename.txt
# Timestamp: 2024-01-15T10:30:45Z
# Model: llama-3-8b-instruct

[LMStudio Response Content Here]
```

## Future Enhancements

1. Support for other archive formats (.zip, .rar)
2. Batch processing of multiple archives in one run
3. JSON/CSV output format options
4. Summary report generation with statistics
5. Integration with file watchers for real-time processing
6. Configuration profiles for different use cases
7. GUI mode for local development

## License and Attribution

This tool is part of the LMStudioClient project and follows its licensing terms. All processing is done through the LMStudio API as configured by the user.