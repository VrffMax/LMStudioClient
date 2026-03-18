# LM Studio Client

A powerful .NET 8 console application that provides a programmatic interface to interact with locally hosted Large Language Models through the [LM Studio API](https://lmstudio.ai).

## Features

- **Local-first AI**: Run LLMs on your machine - no data leaves your computer
- **Real-time Streaming**: Token-by-token response display for interactive chat
- **Conversation History**: Maintain context across multiple messages
- **Advanced Configuration**: Control temperature, max tokens, and model selection
- **Cross-platform**: Works on Windows, Linux, and macOS with .NET 8

## Prerequisites

1. [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
2. [LM Studio](https://lmstudio.ai) installed and running locally (default: `http://localhost:1234`)

## Installation

```bash
cd LibRuWorkspace\LMStudioClient\src
dotnet restore
```

## Quick Start

### List Available Models

```bash
dotnet run --list-models -l
```

### Basic Chat Session

Start an interactive chat with the default model:

```bash
dotnet run
```

Or specify a custom model:

```bash
dotnet run --model "llama-3.2-3b-instruct"
```

### Advanced Configuration

```bash
# Custom system prompt
dotnet run --system "You are a coding assistant specialized in C#"

# Adjust response creativity (0.0 = factual, 2.0 = creative)
dotnet run --temperature 1.5 --max-tokens 1024

# Enable streaming for real-time output
dotnet run --stream

# Custom LM Studio URL
dotnet run --url "http://localhost:1234"
```

### Interactive Mode

The application runs in an interactive loop. Type `/quit`, `/q`, or `exit` to quit:

```
You: What is machine learning?
Assistant: Machine learning is a subset of artificial intelligence...
```

### Piped Input Testing (Automated)

You can test the application with piped input for automated scenarios without manual interaction. This is useful for scripting, CI/CD pipelines, or batch testing.

#### How It Works

When you pipe input to the application:
1. Messages are read sequentially from stdin (standard input)
2. Each line becomes a user prompt in the chat session
3. The application processes each message and waits for LM Studio's response
4. Type `/quit`, `/q`, or `exit` to terminate the session

#### Windows PowerShell Examples

**Basic piped input:**
```powershell
type sample_test_input.txt | dotnet run --project src/
```

**Using compiled executable:**
```powershell
.\publish\win-x64\LMStudioClient.exe < sample_test_input.txt
```

**Quick one-line test with echo:**
```powershell
echo "Hello; exit" | dotnet run --project src/
```

**Note on Windows Platforms:**  
The piped input feature works most reliably in **PowerShell**. When using Git Bash or Windows Command Prompt, you may encounter stdin handling limitations. For best results:
- Use PowerShell for all piped input operations
- Or use the compiled executable directly: `.\LMStudioClient.exe < file.txt`

#### Linux/macOS Examples

**Using bash/cat:**
```bash
cat sample_test_input.txt | ./LMStudioClient
# or
dotnet run --project src/ < sample_test_input.txt
```

**Quick test with echo:**
```bash
echo "Hello; exit" | dotnet run --project src/
```

#### Test Input Files

**Ready-to-Use Example:**  
Create `test_input.txt` with this content for multi-turn conversation testing:

```text
Hello! I'm testing the LM Studio Client.
What is artificial intelligence?
How does machine learning work in practice?
Write a simple Python program to calculate Fibonacci sequence
exit
```

Then run:
```bash
# Windows PowerShell
type test_input.txt | dotnet run --project src/

# Linux/macOS  
cat test_input.txt | ./LMStudioClient
```

Then run:
```bash
cat test_input.txt | dotnet run --project src/
```

#### Piping Input Without Response (Server Unavailable)

When LM Studio server is not running, piped input will show connection errors:

```powershell
echo "test" | dotnet run --project src/
# Output: Error: Cannot connect to LM Studio server at 'http://localhost:1234'
```

This is expected behavior. The application requires a running LM Studio server (on localhost:1234 by default) to process messages and generate responses.

#### Advanced Piped Input Examples

**Test with custom system prompt:**
```powershell
echo "Question: How do I reverse an array in C#?" | dotnet run --project src/ --system "You are a helpful coding assistant that provides complete code examples."
```

**Test with multiple options:**
```bash
{ echo "Hello"; echo "exit" ; } | LMSTUDIO_URL=http://localhost:1234 DEBUG=1 dotnet run --project src/ --temperature 0.7 --max-tokens 512
```

#### Verification Checklist

After running piped input tests, verify:

- [ ] Application starts without hanging indefinitely  
- [ ] Messages are processed in order (first to last)
- [ ] `exit` command terminates cleanly with "Goodbye!" message
- [ ] No zombie processes remain after test completion
- [ ] Error messages display correctly if server unavailable

#### Troubleshooting Piped Input

**Application hangs after reading all input:**  
Make sure to include `exit` at the end of your input. Without it, the application waits for more input indefinitely.

**Piping issues on Windows CMD/Git Bash:**  
When using Windows Command Prompt or Git Bash, stdin redirection may behave unexpectedly due to terminal limitations. Recommended solutions:
1. Use PowerShell instead: `type file.txt \| dotnet run --project src/`
2. Redirect output separately: `echo "test; exit" \| dotnet run --project src/ > output.log 2>&1`
3. For CI/CD pipelines, always use error tolerance: `command || echo "Expected behavior"`

**No output appears for messages:**  
LM Studio server must be running on localhost:1234 (or your specified URL). Verify with:
```bash
dotnet run --list-models -l
```

**Connection refused errors when piping:**  
This is normal if LM Studio isn't running. The application will show a helpful error message with instructions to start it or specify a custom server URL.

**Understanding Exit Codes:**
- `0` = Successful completion or intentional exit via `/quit`, `/q`, or `exit` command  
- Non-zero = Application error, connection refused, or timeout (e.g., server unavailable)  
- `124` = Command timed out (expected when using `timeout` utilities for automated testing)

In CI/CD pipelines, use `|| true` to prevent failures from expected errors:
```bash
echo "test; exit" | dotnet run --project src/ 2>&1 || echo "Expected - server unavailable"
```

## Command-Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--model <name>` | Use specific model (auto-select if omitted) | auto |
| `--system "prompt"` | Set system prompt instruction | You are a helpful AI assistant. |
| `--temperature <0-2.0>` | Response creativity scale | 0.7 |
| `--max-tokens <int>` | Maximum response tokens (capped at 4096) | 512 |
| `--stream` | Enable streaming output | false |
| `--list-models`, `-l` | List available models and exit | - |
| `--url <endpoint>` | Custom LM Studio server URL | http://localhost:1234 |
| `--help`, `-h` | Show help information and exit | - |

### Piped Input Examples

Test the application with piped messages:

```bash
# Windows PowerShell
type test_input.txt | dotnet run --project src/

# Linux/macOS
cat test_input.txt | ./LMStudioClient

# Quick echo test (requires "exit" to terminate)
echo "What is AI?; exit" | dotnet run --project src/
```

## Environment Variables

```bash
# Override default server URL
LMSTUDIO_URL=http://your-server.com:1234 dotnet run

# Enable debug output (shows stack traces)
DEBUG=1 dotnet run --temperature 0.9
```

## Examples

### Multi-turn Conversation with Custom System Prompt

```bash
dotnet run --system "You are a helpful coding assistant. Answer in plain English." --max-tokens 256
```

Sample session:
```
Model: llama-3.2-3b-instruct
Temperature: 0.7 | Max Tokens: 256

───────────────────────────────────────────────────────
You: How do I read a file in C#?
Assistant: You can use File.ReadAllText() from the System.IO namespace...
```

### Streaming Real-time Responses (Long Outputs)

```bash
dotnet run --stream --max-tokens 2048 --temperature 1.0
```

This displays tokens as they're generated, providing immediate feedback on long responses.

## Troubleshooting

### Connection Refused Error

**Error**: `Cannot connect to LM Studio server at 'http://localhost:1234'`

**Solution**: Ensure LM Studio is running and accessible:

```bash
# Check if LM Studio is listening on the correct port
netstat -an | findstr :1234

# If not, start LM Studio or specify custom URL
dotnet run --url "http://your-localhost-ip:1234"
```

### Model Not Found

**Error**: Server returns 404 for specified model

**Solution**: List available models first:

```bash
dotnet run --list-models -l
# Then use one of the listed model names
dotnet run --model "llama-3.2-1b-instruct"
```

### Slow Response Times

If responses take too long, consider:

1. Reducing `--max-tokens` to shorter values (e.g., 256)
2. Lowering `--temperature` for faster completion
3. Ensuring your machine has sufficient RAM for model inference

## Project Structure

```
LMStudioClient/
├── src/
│   ├── LMStudioClient.csproj      # .NET project file
│   └── Program.cs                 # Main application code (~350 lines)
├── docs/
│   ├── PROJECT_SPECIFICATION.md   # Technical specification
│   └── PROJECT_ANALYSIS.md        # Feasibility analysis
├── PIPE_INPUT_TESTING.md          # Comprehensive piped input testing guide (675 lines)
├── test_input.txt                 # Sample piped input for testing
├── sample_test_input.txt          # Example test messages file  
├── run_test.bat                   # Windows batch test script
├── test_input.ps1                 # PowerShell automation test script
└── README.md                      # This user documentation file

```

## Build for Distribution

### Create Self-contained Executable (Windows)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish/win-x64
```

The published executable is located at `publish/win-x64/LMStudioClient.exe` and includes all dependencies. You can distribute this folder directly to users without requiring .NET 8 installed on their machines.

**Verify the build succeeded:**
```bash
# Check that required files were created
dir publish\win-x64\*.exe publish\win-x64\*.dll >nul 2>&1

# Run help command to verify it works (should show application info)
.\publish\win-x64\LMStudioClient.exe --help -l 2>&1 | Select-Object -First 10
```

**Distribute the entire `publish/win-x64/` folder** to users. No .NET runtime installation required!

### Create Self-contained Executable (Linux/macOS)

```bash
# For Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish/linux-x64

# For macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/osx-arm64

# For macOS x64 (Intel Macs)
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/osx-x64
```

**Verify the build succeeded:**
```bash
# Check that required files were created
ls publish/linux-x64/*.exe publish/linux-x64/*.dll 2>/dev/null || echo "Check path"

# Run help command to verify it works (should show application info)
./publish/linux-x64/LMStudioClient --help -l 2>&1 | head -10
```

**Distribute the entire `publish/[platform]/` folder** to users. No .NET runtime installation required!

### Create Docker Image (Optional)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LMStudioClient.dll"]
```

## Security Considerations

- ✅ **Zero external network calls** - All traffic stays local (localhost only)
- ✅ **No telemetry** - No analytics or usage tracking
- ✅ **Memory-only sessions** - Conversation history never persisted to disk
- ⚠️ **Use HTTPS if available** - For production deployments, ensure LM Studio uses TLS

## Performance Tips

1. **Connection Pooling**: Reuses HTTP connections for faster repeated requests
2. **Streaming**: Prefer `--stream` for long outputs to avoid buffering delays
3. **Context Window**: Keep conversation history reasonable (default handles typical use)
4. **Temperature Tuning**: Lower values (0.5-0.7) produce faster, more deterministic responses



## Build Checklist

Before distributing or deploying, verify:

- [ ] Project builds successfully: `dotnet build --configuration Release`
- [ ] Executable created in publish folder
- [ ] Running `--help` shows application information
- [ ] Piped input test works (with LM Studio running)
- [ ] Error handling displays helpful messages when server unavailable

**Windows:**
```bash
cd src/publish/win-x64
.\LMStudioClient.exe --help -l 2>&1 | Select-Object -First 5
```

**Linux/macOS:**
```bash
cd src/publish/linux-x64
./LMStudioClient --help -l 2>&1 | head -5
```

## Contributing

This project is designed as a learning and development tool. Feel free to:

1. Submit bug reports for compilation issues or runtime errors
2. Suggest new features in the documentation
3. Fork and extend with custom plugins

---

## License

This project is licensed under the MIT License - see the LICENSE file (if exists) or use this text:

```
MIT License

Copyright (c) 2026 LM Studio Client Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Credits
## Credits

- [LM Studio](https://lmstudio.ai) - Local LLM inference server
- .NET 8 Runtime - Modern C# framework by Microsoft

## Support

For issues, questions, or feature requests:
1. Check the troubleshooting section above
2. Review PROJECT_SPECIFICATION.md for technical details
3. Contact the development team (if applicable)

---
- .NET 8 Runtime - Modern C# framework by Microsoft

## Support

For issues, questions, or feature requests:
1. Check the troubleshooting section above
2. Review PROJECT_SPECIFICATION.md for technical details
3. Contact the development team (if applicable)

---

## Quick Reference

| Task | Command |
|------|---------|
| **List models** | `dotnet run --list-models -l` |
| **Start interactive chat** | `dotnet run` |
| **Use custom model** | `dotnet run --model "llama-3.2-1b"` |
| **Piped input (PowerShell)** | `type test.txt \| dotnet run --project src/` |
| **Piped input (Linux/macOS)** | `cat test.txt \| ./LMStudioClient` |
| **Custom system prompt** | `dotnet run --system "You are a coding assistant"` |
| **Adjust temperature/tokens** | `dotnet run --temperature 0.5 --max-tokens 256` |
| **Enable streaming output** | `dotnet run --stream` |
| **Debug mode (show stack traces)** | `DEBUG=1 dotnet run` |
| **Custom server URL** | `dotnet run --url "http://localhost:1234"` |

---

**Version**: 1.0.0  
**Build Date**: March 20, 2026  
**Status**: Phase 1 MVP Complete ✅  
**Platform**: .NET 8.0 Console Application  
---

## Exit Codes Reference

| Code | Meaning | Action Required |
|------|---------|-----------------|
| `0` | Success or intentional exit (`/quit`, `/q`, `exit`) | None - normal completion |
| Non-zero (e.g., 1) | Application error, connection refused, or server unavailable | Check troubleshooting section for specific error messages |
| `124` | Command timed out (when using `timeout` utilities) | Expected in automated testing with long responses; increase timeout if needed |

**CI/CD Best Practice:** Always use `|| true` to prevent pipeline failures from expected errors:
```bash
echo "test; exit" | dotnet run --project src/ 2>&1 || echo "Expected - server unavailable or timeout"
```

> 💡 **Pro Tip**: Always include `exit` in piped input to prevent hanging! 🚀

---

**Version**: 1.0.0  
**Build Date**: March 20, 2026  
**Status**: Phase 1 MVP Complete ✅  
**Platform**: .NET 8.0 Console Application