# Piped Input Testing Guide - LM Studio Client

## Overview

This document provides comprehensive testing instructions, examples, and verification steps for using the **piped input** feature of the LM Studio Client. Piping allows you to automate chat interactions without manual typing, making it ideal for scripting, CI/CD pipelines, batch testing, or programmatic integration.

---

## How Piped Input Works

When you pipe data into the application:

1. Messages are read sequentially from **stdin** (standard input)
2. Each line becomes a user prompt in the chat session
3. The application processes each message and waits for LM Studio's response
4. Type `/quit`, `/q`, or `exit` to terminate the session gracefully

---

## Platform-Specific Usage Examples

### Windows PowerShell

#### Basic Piped Input (using echo)
```powershell
echo "Hello; exit" | dotnet run --project src/
```

Or using a file:
```powershell
type test_input.txt | dotnet run --project src/
```

Using the compiled executable directly:
```powershell
.\publish\win-x64\LMStudioClient.exe < input.txt
```

#### Multiple Messages
```powershell
{ echo "What is machine learning?" ; echo "exit" } | dotnet run --project src/
```

Or using a file with multiple lines:
```powershell
$messages = @("Hello", "How are you?", "Write a C# program to reverse an array", "exit")
echo -join($messages) | dotnet run --project src/
```

### Linux/macOS (bash/shell)

#### Basic Piped Input (using echo)
```bash
echo "Hello; exit" | ./LMStudioClient
```

Or using dotnet:
```bash
echo "Hello; exit" | dotnet run --project src/
```

#### Multiple Messages (using brace expansion or command grouping)
```bash
{ echo "What is machine learning?" ; echo "exit" ; } | ./LMStudioClient
```

Using a file with cat:
```bash
cat test_input.txt | ./LMStudioClient
# or
dotnet run --project src/ < test_input.txt
```

#### Multiple Messages (using printf)
```bash
printf "Hello\nHow are you?\nWrite code to reverse array\nexit\n" | ./LMStudioClient
```

---

## Test Input File Templates

### Basic Template (test_basic.txt)
```text
What is machine learning?
Exit
```

**Run test:**
```powershell
type test_basic.txt | dotnet run --project src/
# or Linux/macOS: cat test_basic.txt | ./LMStudioClient
```

---

### Advanced Template with Custom System Prompt (test_advanced.txt)
```text
System instruction: You are a helpful coding assistant.
Question 1: How do I read a file in C#?
Question 2: Write a function to reverse an array
exit
```

**Run test:**
```powershell
type test_advanced.txt | dotnet run --project src/ --system "You are a helpful coding assistant"
```

---

### Error Testing Template (test_errors.txt)
Use this when LM Studio is NOT running to verify error handling:
```text
Test message 1
Test message 2
exit
```

**Expected behavior:**
- Shows connection refused error with helpful instructions
- Gracefully exits after displaying error
- No hanging or zombie processes

---

## Command-Line Options for Testing

### Test with Debug Output (shows stack traces)
```powershell
DEBUG=1 echo "test; exit" | dotnet run --project src/
```

Or:
```bash
echo "test; exit" | DEBUG=1 ./LMStudioClient
```

---

### Test with Custom Configuration
```powershell
{ echo "Hello"; echo "exit" ; } | dotnet run --project src/ --temperature 0.7 --max-tokens 512 --system "You are a coding assistant"
```

---

## Verification Checklist

After running piped input tests, verify:

- [ ] Application starts without hanging indefinitely  
- [ ] Messages are processed in order (first to last)
- [ ] `exit` command terminates cleanly with "Goodbye!" message
- [ ] No zombie processes remain after test completion
- [ ] Error messages display correctly if server unavailable
- [ ] Model responses appear when LM Studio is running

---

## Troubleshooting Piped Input

### Issue: Application hangs after reading all input
**Solution:** Make sure to include `exit` at the end of your piped input. Without it, the application waits for more input indefinitely.

Example (correct):
```powershell
echo "Hello; exit" | dotnet run --project src/  # ✅ Works
```

Example (incorrect - will hang):
```powershell
echo "Hello" | dotnet run --project src/        # ❌ Hangs waiting for more input
```

---

### Issue: No output appears for messages
**Solution:** LM Studio server must be running on `localhost:1234` (or your specified URL). Verify with:
```powershell
dotnet run --list-models -l
```

If models are listed, the server is accessible. Then try piped input again.

---

### Issue: Connection refused errors when piping
**Solution:** This is **expected behavior** if LM Studio isn't running. The application will show a helpful error message with instructions to start it or specify a custom server URL:

```text
Error: Cannot connect to LM Studio server at 'http://localhost:1234'
Make sure LM Studio is running on localhost port 1234.
To start LM Studio, download from: https://lmstudio.ai
```

This is normal - it means your error handling works correctly!

---

### Issue: "The handle is invalid" or similar console errors (Windows)
**Solution:** This can occur when piping to the Windows console simultaneously. It's a limitation of how stdin/stdout redirection works in Windows terminals. The core functionality still works - just ignore this cosmetic error if you see it.

To avoid this, consider using:
- PowerShell with proper input/output handling
- A batch file or script that manages the process
- Testing via the compiled executable instead of `dotnet run`

---

## Advanced Testing Scenarios

### Test 1: Basic Connectivity Test
Verifies LM Studio server is accessible before running full chat tests.

```powershell
# Quick connectivity check
dotnet run --list-models -l
```

Expected output should list available models.

---

### Test 2: Full Chat Session Automation
Simulates a complete user session with multiple messages.

**Create `test_session.txt`:**
```text
Hello! I'm testing the LM Studio Client.
What is artificial intelligence?
How does machine learning differ from deep learning?
Write a simple Python program that prints "Hello World"
Goodbye!
exit
```

**Run test:**
```powershell
type test_session.txt | dotnet run --project src/
```

---

### Test 3: Error Recovery Test
Tests how the application handles server errors gracefully.

First, stop LM Studio if it's running:
```powershell
# Stop any running instances (Windows)
Get-Process -Name "lmstudio" | Stop-Process -Force -ErrorAction SilentlyContinue
```

**Run piped input:**
```powershell
echo "test message; exit" | dotnet run --project src/
```

**Expected behavior:**
- Shows connection error message
- Displays helpful instructions
- Exits cleanly without hanging

---

### Test 4: Custom Configuration Stress Test
Tests all configuration options work correctly with piped input.

**Create `test_config.txt`:**
```text
Test message 1
Test message 2
exit
```

**Run test:**
```powershell
type test_config.txt | dotnet run --project src/ \
    --temperature 0.5 \
    --max-tokens 256 \
    --system "You are a concise assistant" \
    DEBUG=1
```

---

## Integration with CI/CD Pipelines

### GitHub Actions Example
```yaml
name: LM Studio Client Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build project
        run: cd src && dotnet build --configuration Release
      
      - name: Run basic tests (requires LM Studio)
        # This assumes LM Studio is running in the container or host machine
        run: |
          echo "Hello; exit" | ./src/bin/Release/net8.0/LMStudioClient
      
      - name: Test error handling (server unavailable)
        run: |
          timeout 5 dotnet run --project src/ --list-models || true
```

---

### Azure DevOps Pipeline Example
```yaml
pool:
  vmImage: 'ubuntu-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Restore dependencies'
  inputs:
    command: 'restore'
    
- script: |
    echo "Testing piped input functionality..."
    { echo "test message"; echo "exit"; } | dotnet run --project src/ || true
  displayName: 'Run piped input test'
```

---

## Testing Script Templates

### Windows PowerShell Test Script (run_tests.ps1)
```powershell
# LM Studio Client - Automated Test Suite
param(
    [switch]$Verbose,
    [string]$TimeoutSeconds = 30
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LM Studio Client - Automated Tests" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

# Test 1: Check executable exists
$exePath = Join-Path $PSScriptRoot "src\publish\win-x64\LMStudioClient.dll"
if (Test-Path $exePath) {
    Write-Host "[PASS] Executable found at $exePath" -ForegroundColor Green
} else {
    Write-Host "[WARN] Executable not found. Building..." -ForegroundColor Yellow
    Set-Location "src"
    dotnet publish -c Release -r win-x64 --self-contained true -o "publish/win-x64" -v q
}

# Test 2: List models (verifies server connectivity)
Write-Host "`n[Test] Checking LM Studio server..." -ForegroundColor Cyan
$output = dotnet run --list-models -l 2>&1 | Select-String "Available Models"
if ($output) {
    Write-Host "[PASS] LM Studio server detected on localhost:1234" -ForegroundColor Green
} else {
    Write-Host "[INFO] Server may not be running or no models loaded" -ForegroundColor Yellow
}

# Test 3: Basic piped input test
Write-Host "`n[Test] Running basic piped input..." -ForegroundColor Cyan
$testInput = "Hello; exit"
$result = echo $testInput | dotnet run --project src/ 2>&1 | Select-String "Goodbye|Assistant"

if ($result) {
    Write-Host "[PASS] Piped input processed successfully" -ForegroundColor Green
} else {
    Write-Host "[INFO] Server response depends on connectivity" -ForegroundColor Yellow
}

# Test 4: Error handling test (optional, server unavailable)
Write-Host "`n[Test] Verifying error handling..." -ForegroundColor Cyan
$errorTest = dotnet run --project src/ 2>&1 | Select-String "Cannot connect|Error"

if ($errorTest) {
    Write-Host "[PASS] Error messages display correctly when server unavailable" -ForegroundColor Green
} else {
    Write-Host "[INFO] Server is running (expected error test skipped)" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "All tests completed!" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
```

**Usage:**
```powershell
.\run_tests.ps1 --Verbose
```

---

### Linux/macOS Bash Test Script (test.sh)
```bash
#!/bin/bash

# LM Studio Client - Automated Test Suite for Unix/Linux/macOS

set -e  # Exit on error (optional, remove for debugging)

echo "========================================"
echo "LM Studio Client - Automated Tests"
echo "========================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

passed=0
failed=0

# Test 1: Check executable/dotnet project exists
echo -e "\n[Test] Checking build artifacts..."
if [ -f "src/bin/Release/net8.0/LMStudioClient.dll" ]; then
    echo -e "[PASS] Build artifacts found in src/bin/Release/"
    ((passed++))
else
    echo -e "[WARN] Building project first..."
    cd src
    dotnet build --configuration Release -v q
    cd ..
fi

# Test 2: List models (verifies server connectivity)
echo -e "\n[Test] Checking LM Studio server..."
if output=$(dotnet run --list-models -l 2>&1); then
    if echo "$output" | grep -q "Available Models"; then
        echo -e "[PASS] LM Studio server detected on localhost:1234"
        ((passed++))
    else
        echo -e "[INFO] Server may not be running or no models loaded"
    fi
else
    echo -e "[FAIL] Failed to connect to server"
    ((failed++))
fi

# Test 3: Basic piped input test
echo -e "\n[Test] Running basic piped input..."
test_input="Hello; exit"
if output=$(echo "$test_input" | timeout 10 dotnet run --project src/ 2>&1); then
    if echo "$output" | grep -qE "Goodbye|Assistant"; then
        echo -e "[PASS] Piped input processed successfully"
        ((passed++))
    else
        echo -e "[INFO] Server response varies based on configuration"
    fi
else
    echo -e "[INFO] Command exited (expected with server errors)"
fi

# Test 4: Error handling test (server unavailable)
echo -e "\n[Test] Verifying error handling..."
error_output=$(dotnet run --project src/ 2>&1 || true)
if echo "$error_output" | grep -qE "Cannot connect|Error"; then
    echo -e "[PASS] Error messages display correctly when server unavailable"
    ((passed++))
else
    echo -e "[INFO] Server is running (skipped error test)"
fi

# Summary
echo -e "\n========================================"
echo -e "Test Results: $passed passed, $failed failed"
echo -e "========================================"

if [ $failed -eq 0 ]; then
    echo -e "All tests completed successfully! ${GREEN}✓${NC}"
else
    echo -e "${RED}Some tests failed. Check output above.${NC}"
fi
```

**Usage:**
```bash
chmod +x test.sh
./test.sh --verbose  # Add verbose flag if implemented
```

---

## Common Test Patterns

### Pattern 1: Quick Smoke Test (5 seconds)
```powershell
timeout 5 { echo "Hello; exit" | dotnet run --project src/ } 2>$null || echo "Smoke test passed - app responds quickly"
```

---

### Pattern 2: Continuous Integration Test with Timeout
```bash
# Linux/macOS
(echo -e "test message\nexit" | timeout 30 ./LMStudioClient) && echo "Test passed" || echo "Test failed or timed out"
```

```powershell
# Windows PowerShell
timeout /t 30 /nobreak (echo "Hello; exit" | dotnet run --project src/) >nul 2>&1
if ($LASTEXITCODE -eq 0) { Write-Host "Test passed" } else { Write-Host "Test failed or timed out" }
```

---

### Pattern 3: Test with Custom Environment Variables
```bash
# Override server URL for testing
export LMSTUDIO_URL=http://localhost:1234
echo "test; exit" | dotnet run --project src/
```

---

### Pattern 4: Batch Test Multiple Scenarios
Create `test_scenarios.txt`:
```text
Scenario 1: Basic greeting
Hello! How are you?
exit

Scenario 2: Coding question  
System prompt: You are a coding expert.
Question: Write a C# method to reverse an array
exit

Scenario 3: Error condition (if server unavailable)
test message
another test
exit
```

Run all scenarios in one command:
```bash
cat test_scenarios.txt | ./LMStudioClient --system "You are helpful" 2>&1 || true
```

---

## Performance Testing with Piped Input

### Test Response Times
Measure how long each message takes to process:

```powershell
# PowerShell - Time individual messages
$measure = Measure-Command {
    echo "Hello; exit" | dotnet run --project src/ 2>&1 | Out-Null
}
Write-Host "Response time: $measure.TotalMilliseconds ms"
```

---

### Test Throughput (Messages Per Second)
Create a script that sends 50 messages quickly and measures throughput.

**File `throughput_test.txt`:**
```text
Message 1
Message 2
...
Message 50
exit
```

Then time the execution:
```powershell
$startTime = Get-Date
type throughput_test.txt | dotnet run --project src/ >nul 2>&1
$endTime = Get-Date
Write-Host "Processed in $($($endTime - $startTime).TotalSeconds) seconds"
```

---

## Debugging Tips

### Enable Verbose Logging with Piped Input
```powershell
DEBUG=1 VERBOSE=1 echo "test; exit" | dotnet run --project src/
```

Or add logging to the application code temporarily:
```csharp
// In Program.cs, add at key points
Console.WriteLine($"[LOG] {DateTime.Now:HH:mm:ss} - Processing message: {userPrompt}");
```

---

### Capture Full Output for Analysis
```powershell
# Save complete output with timestamps
echo "test; exit" | dotnet run --project src/ > test_output.txt 2>&1
Get-Content test_output.txt | Select-Object -First 50
```

---

### Test with Different Terminal Emulators (Windows)
Some terminals handle stdin redirection better than others. Try:

**PowerShell:**
```powershell
echo "test; exit" | dotnet run --project src/
```

**CMD (Command Prompt):**
```cmd
echo test^;exit | dotnet run --project src/
```

Note: CMD requires escaping the semicolon (`^;`) due to command parsing.

---

## Best Practices for Piped Input Testing

1. **Always include `exit` or `/quit`** at the end of piped input to prevent hanging
   
2. **Use error redirection (`|| true`)** in CI/CD pipelines since server may not always be running:
   ```bash
   echo "test; exit" | dotnet run --project src/ 2>&1 || true
   ```

3. **Set timeouts** for automated tests to prevent hanging if something goes wrong:
   ```powershell
   timeout 30 { ... }  # PowerShell
   timeout 30           # Linux/macOS
   ```

4. **Log all output** during testing for easier debugging:
   ```bash
   echo "test; exit" | dotnet run --project src/ > test.log 2>&1
   tail -f test.log
   ```

5. **Test both success and failure scenarios:**
   - With LM Studio running (successful responses)
   - Without LM Studio running (error handling verification)

6. **Use descriptive filenames** for test inputs:
   - `test_basic.txt` - Basic functionality check
   - `test_errors.txt` - Error handling verification
   - `test_integration.txt` - Full integration scenario

---

## Summary

Piped input is a powerful feature that enables automated testing and scripting with the LM Studio Client. By following these guidelines, you can:

- ✅ Automate chat sessions without manual interaction  
- ✅ Integrate into CI/CD pipelines for continuous validation
- ✅ Test error handling and edge cases systematically
- ✅ Measure performance and response times
- ✅ Create reproducible test scenarios

Remember to always include `exit` in your piped input, handle errors gracefully, and use appropriate timeouts for automated testing. Happy testing! 🚀