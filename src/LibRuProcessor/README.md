# LibRuProcessor - .7z Archive Processor with LMStudio Integration

## Overview

LibRuProcessor is a powerful tool that scans `.7z` archive files, extracts all text (`.txt`) files from the archives, reads the first 4096 bytes of each file, processes them through the LMStudio API for analysis and summarization, and writes the processed responses to new files with an `lms` suffix.

## Features

- **Archive Processing**: Automatically scans directories or single `.7z` files
- **Text File Extraction**: Extracts all `.txt` files from archives
- **Content Sampling**: Reads exactly 4096 bytes (or less if file is smaller) from each text file
- **LMStudio Integration**: Sends content to LMStudio API for intelligent processing
- **Smart Output**: Writes results with `lms` suffix in a separate output directory
- **Batch Processing**: Supports sequential or parallel processing of multiple archives
- **Dry Run Mode**: Preview files to be processed without executing
- **Configurable Parameters**: Custom temperature, tokens, model selection

## Quick Start

### Basic Usage

```bash
# Process all .7z archives in current directory
dotnet run --project src/LibRuProcessor/LibRuProcessor.csproj -- --input ./archives/

# Process a specific archive file
dotnet run --project src/LibRuProcessor/LibRuProcessor.csproj -- --input document.7z

# Dry-run to see what will be processed
dotnet run --project src/LibRuProcessor/LibRuProcessor.csproj -- --input ./archives/ --dry-run
```

### Advanced Options

```bash
# Process with custom settings and parallel execution
dotnet run --project src/LibRuProcessor/LibRuProcessor.csproj -- \
  --input ./archives/ \
  --output ./results/ \
  --model "llama-3-8b-instruct" \
  --temperature 0.5 \
  --max-tokens 1024 \
  --parallel \
  --conc 4

# Process single archive with specific LMStudio URL
dotnet run --project src/LibRuProcessor/LibRuProcessor.csproj -- \
  --input archive.7z \
  --url http://localhost:1234/v1 \
  --verbose
```

## Output Format

Each processed file will be saved with the format: `{original_name}.txt.lms`

The output files contain:
- Processing metadata (timestamp, model used)
- Original filename reference
- LMStudio API response content

Example structure:
```
processed/
├── report_2024.txt.lms
├── notes_summary.txt.lms  
└── document_analysis.txt.lms
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `LMSTUDIO_URL` | LMStudio API base URL | `http://localhost:1234/v1` |
| `LMSTUDIO_MODEL` | Model name to use | Auto-select first available |
| `LMSTUDIO_TEMPERATURE` | Response creativity (0.0-2.0) | `0.7` |
| `LMSTUDIO_MAX_TOKENS` | Maximum response tokens (32-4096) | `512` |

### Command-Line Options

```
--input <path>          Input directory or single .7z archive (required)
--output <path>         Output directory for processed files (default: ./processed/)
--url <string>          LMStudio API URL override
--model <name>          Specific model to use
--temperature <float>   Temperature 0-2.0 (default: 0.7)
--max-tokens <int>      Maximum response tokens (default: 512, max: 4096)
--stream                Enable streaming output mode
--verbose               Show detailed progress information
--dry-run               List files to process without executing
--parallel              Process archives in parallel mode
--conc <int>            Maximum concurrent operations (default: 4)
```

## Error Handling

- **Missing Archives**: Logs warning, continues with other files
- **Invalid Archive Format**: Skips file and logs error
- **API Connection Failed**: Retries up to 3 times with exponential backoff
- **File Too Large**: Only processes first 4096 bytes (as designed)
- **Rate Limiting**: Implements automatic retry strategy

## Performance

- Sequential mode: Safe for large archives, one file at a time
- Parallel mode: Up to 8 concurrent operations (configurable via `--conc`)
- Automatic cleanup of temporary extraction files
- Memory-efficient streaming for content processing

## Security

- Input path validation prevents directory traversal attacks
- No sensitive data logged or stored
- API credentials never written to disk
- Temporary files cleaned up after successful processing

## Troubleshooting

### Common Issues

**Problem**: "Archive not found"
- **Solution**: Verify the `.7z` file exists and path is correct

**Problem**: "LMStudio connection failed"  
- **Solution**: Check if LMStudio server is running on default port 1234 or specified URL

**Problem**: Empty output files
- **Solution**: Ensure input .txt files are not empty or contain only whitespace

**Problem**: Parallel processing too slow
- **Solution**: Reduce `--conc` value to match your API rate limits

## Project Structure

```
LibRuProcessor/
├── LibRuProcessor.csproj      # Project file with SevenZipLib.NET dependency
├── Program.cs                 # Main entry point and CLI parser
├── LibRuProcessorService.cs   # Core processing logic
├── LibRuBatchProcessor.cs     # Batch processing capabilities
└── README.md                  # This documentation
```

### Dependencies
+- **SevenZipSharp** - For .7z archive extraction (v20.1.0+ required)
   - Uncomment in `LibRuProcessor.csproj` when ready: 
   - `<PackageReference Include="SevenZipSharp" Version="20.1.0" />`
+- **LMStudioClient.Services** - Reuses existing API service layer
+- **.NET 8 Runtime** - Required for execution

## License

This tool is part of the LMStudioClient project and follows its licensing terms.

### Future Enhancements
+Planned features:
+- [x] **Immediate**: Add SevenZipSharp package and enable .7z support
   - Uncomment line in `LibRuProcessor.csproj` to activate
+- [ ] Support for additional archive formats (.zip, .rar)
+- [ ] JSON/CSV output format options  
+- [ ] Batch processing of multiple archives in a single run
+- [ ] Summary report generation with statistics
+- [ ] Configuration profiles for different use cases
+- [ ] GUI mode for local development
+- [ ] File watcher integration for real-time processing

## Contributing

Contributions are welcome! Please ensure:
1. All new features include comprehensive tests
2. Documentation is updated accordingly  
3. No build artifacts or temporary files are committed
4. Follow existing code style and patterns

For more information, see the main [LMStudioClient documentation](../README.md).