@echo off
REM Batch script to run UI tests for Daily Task Manager Avalonia application

echo 🚀 Daily Task Manager UI Test Runner
echo.

REM Set default configuration
set CONFIG=Debug
if not "%1"=="" set CONFIG=%1

echo Configuration: %CONFIG%
echo Solution Root: %~dp0..\..
echo.

REM Step 1: Build the Avalonia application
echo 📦 Building Avalonia application...
cd /d "%~dp0..\..\src\DailyTaskManager.Avalonia"

echo Restoring packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ❌ Failed to restore packages
    pause
    exit /b 1
)

echo Building application...
dotnet build -c %CONFIG% --no-restore
if %ERRORLEVEL% neq 0 (
    echo ❌ Failed to build Avalonia application
    pause
    exit /b 1
)

echo ✅ Avalonia application built successfully
echo.

REM Step 2: Kill existing application instances
echo 🔪 Cleaning up existing application instances...
taskkill /f /im DailyTaskManager.Avalonia.exe >nul 2>&1
timeout /t 2 >nul

REM Step 3: Build and run tests
echo 🧪 Running UI tests...
cd /d "%~dp0"

dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ❌ Failed to restore test packages
    pause
    exit /b 1
)

dotnet build -c %CONFIG% --no-restore
if %ERRORLEVEL% neq 0 (
    echo ❌ Failed to build test project
    pause
    exit /b 1
)

dotnet test -c %CONFIG% --no-build --no-restore --settings runsettings.xml --logger "console;verbosity=detailed" --logger "trx;LogFileName=UITestResults.trx"

REM Clean up any remaining processes
taskkill /f /im DailyTaskManager.Avalonia.exe >nul 2>&1

echo.
echo 📊 Test results available in: %~dp0TestResults
echo 🏁 UI test run completed
echo.
pause