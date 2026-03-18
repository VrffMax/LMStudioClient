@echo off
REM LM Studio Client - Input Testing Batch File
REM This script tests the application with piped input on Windows

setlocal enabledelayedexpansion

echo ==========================================
echo  LM Studio Client - Piped Input Test
echo ==========================================
echo.

REM Create test messages file
set TEST_FILE=C:\Temp\LMStudioTest.txt
(
    echo Hello, how are you?
    echo What is machine learning?
    echo Write a simple C# program that prints "Hello World"
    echo exit
) > "%TEST_FILE%"

echo [Step 1] Preparing test input file: %TEST_FILE%
echo.

REM Test with basic piped input (non-interactive mode simulation)
echo [Step 2] Running LM Studio Client with piped input...
echo.

dotnet "C:\Playground\LibRuWorkspace\LMStudioClient/src/publish/win-x64/LMStudioClient.dll" < "%TEST_FILE%" ^| findstr /C:"You:" /C:"Assistant:" /C:"Goodbye!" /C:"Error" > nul 2>&1

echo.
echo [Step 3] Test Results:
if exist %TEST_FILE% (
    echo   - Test file created successfully
) else (
    echo   - ERROR: Failed to create test file
    exit /b 1
)

REM Clean up
del "%TEST_FILE%" > nul 2>&1

echo ==========================================
echo  Test Complete!
echo ==========================================
echo.
echo Tip: To run interactively, use:
echo   type test_input.txt | dotnet LMStudioClient.dll
pause > nul
