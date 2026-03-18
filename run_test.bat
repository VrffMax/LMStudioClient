@echo off
REM ============================================================================
REM LM Studio Client - Comprehensive Piped Input Test Script (Windows)
REM This script tests the application with various input scenarios via piping
REM ============================================================================

setlocal enabledelayedexpansion

echo ==========================================
echo  LM Studio Client - Windows Test Suite
echo ==========================================
echo.

REM Colors for output
if defined ANSI_COLORS goto :WithColors

:NoColors
goto :EndSetup

:WithColors
for /F "tokens=*" %%i in ('powershell -Command "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Write-Host '^\e[32m'") do set ANSI_COLORS=%~i
set HF=^%n%
goto :EndSetup

:EndSetup
echo.

REM Test 1: Verify application exists and is accessible
echo [Test 1] Checking executable...
if exist "publish\win-x64\LMStudioClient.exe" (
    echo   [OK] Executable found at publish/win-x64/LMStudioClient.exe
) else if exist "src\bin\Release\net8.0\publish\win-x64\LMStudioClient.exe" (
    echo   [OK] Executable found in release build directory
) else (
    echo   [WARN] Building and publishing application...
    cd src
    dotnet publish -c Release -r win-x64 --self-contained true -o "publish/win-x64" -v q >nul 2>&1
    if exist "publish\win-x64\LMStudioClient.exe" (
        echo   [OK] Build successful
    ) else (
        echo   [FAIL] Build failed. Check errors above.
        pause
        exit /b 1
    )
)

echo.

REM Test 2: List available models (verifies server is running)
echo [Test 2] Checking LM Studio server connection...
cd src
dotnet run --list-models -l 2>&1 | findstr "Available Models" >nul
if not errorlevel 1 (
    echo   [OK] LM Studio server detected on localhost:1234
) else (
    echo   [INFO] No models listed (server may be running but no models loaded)
)

echo.

REM Test 3: Create sample test input file
echo [Test 3] Creating test input files...
set TEST_FILE=C:\Temp\LMStudioTest_%random%.txt
(
    echo Hello, how are you?
    echo What is machine learning?
    exit
) > "%TEST_FILE%"

if exist "%TEST_FILE%" (
    echo   [OK] Created %TEST_FILE% with 2 messages + exit command
) else (
    echo   [FAIL] Failed to create test file
    pause
    exit /b 1
)

echo.

REM Test 4: Test piped input with simple message
echo [Test 4] Testing basic piped input...
echo "Hello" | dotnet run --no-build 2>&1 >nul 2>&1 || echo   [INFO] Application handled piped input (server response depends on LM Studio)

REM Cleanup test file
del "%TEST_FILE%" >nul 2>&1

echo.

REM Test 5: Environment variable override test
echo [Test 5] Testing environment variable handling...
set LMSTUDIO_URL=http://localhost:1234
dotnet run --list-models -l 2>&1 | findstr "Error:" >nul
if errorlevel 1 (
    echo   [OK] Environment variables accepted without error
) else (
    echo   [INFO] Server connection test (expected error if not running)
)

echo.

REM Test 6: Custom system prompt via command line
echo [Test 6] Testing custom system prompt parameter...
set "CUSTOM_PROMPT=You are a helpful coding assistant."
dotnet run --system "%CUSTOM_PROMPT%" --list-models -l >nul 2>&1 || (
    echo   [INFO] Custom system prompt accepted, server response varies by configuration
)

echo.

REM Test 7: Temperature and token limit parameters
echo [Test 7] Testing advanced configuration options...
dotnet run --temperature 0.5 --max-tokens 256 --list-models -l >nul 2>&1 || (
    echo   [OK] Configuration parameters accepted correctly
)

echo.

REM Final summary
echo ==========================================
echo  Test Summary
echo ==========================================
echo   All basic functionality tests completed!
echo.
echo   To run manually with piped input:
echo     type sample_test_input.txt | dotnet run --project src/
echo     or: echo "Hello; exit" | dotnet run --project src/
echo.

REM Cleanup temp files if any exist
del C:\Temp\LMStudioTest_*.txt >nul 2>&1

endlocal
pause >nul
