# PowerShell script to run UI tests for Daily Task Manager Avalonia application
param(
    [string]$Configuration = "Debug",
    [string]$TestFilter = "",
    [switch]$BuildFirst = $true,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Join-Path $scriptPath "..\.."
$avaloniaProjectPath = Join-Path $solutionRoot "src\DailyTaskManager.Avalonia"
$testProjectPath = Join-Path $scriptPath "DailyTaskManager.UI.Tests.csproj"
$runSettingsPath = Join-Path $scriptPath "runsettings.xml"

Write-Host "🚀 Daily Task Manager UI Test Runner" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Solution Root: $solutionRoot" -ForegroundColor Yellow
Write-Host "Test Project: $testProjectPath" -ForegroundColor Yellow

if ($TestFilter) {
    Write-Host "Test Filter: $TestFilter" -ForegroundColor Yellow
}

# Step 1: Build the Avalonia application if requested
if ($BuildFirst) {
    Write-Host "`n📦 Building Avalonia application..." -ForegroundColor Green
    
    # Restore packages first
    Write-Host "Restoring NuGet packages..."
    dotnet restore $avaloniaProjectPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to restore packages" -ForegroundColor Red
        exit 1
    }
    
    # Build the application
    Write-Host "Building application..."
    dotnet build $avaloniaProjectPath -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to build Avalonia application" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Avalonia application built successfully" -ForegroundColor Green
}

# Step 2: Kill any existing application instances
Write-Host "`n🔪 Cleaning up existing application instances..." -ForegroundColor Yellow
$processes = Get-Process -Name "DailyTaskManager.Avalonia" -ErrorAction SilentlyContinue
if ($processes) {
    $processes | Stop-Process -Force
    Write-Host "Stopped $($processes.Count) existing processes"
    Start-Sleep -Seconds 2
}

# Step 3: Restore test project packages
Write-Host "`n📦 Restoring test project packages..." -ForegroundColor Green
dotnet restore $testProjectPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore test packages" -ForegroundColor Red
    exit 1
}

# Step 4: Build test project
Write-Host "`n🔨 Building test project..." -ForegroundColor Green
dotnet build $testProjectPath -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build test project" -ForegroundColor Red
    exit 1
}

# Step 5: Verify Avalonia executable exists
$avaloniaExe = Join-Path $avaloniaProjectPath "bin\$Configuration\net8.0\DailyTaskManager.Avalonia.exe"
if (-not (Test-Path $avaloniaExe)) {
    Write-Host "❌ Avalonia executable not found at: $avaloniaExe" -ForegroundColor Red
    Write-Host "Please build the Avalonia project first" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found Avalonia executable: $avaloniaExe" -ForegroundColor Green

# Step 6: Run the UI tests
Write-Host "`n🧪 Running UI tests..." -ForegroundColor Green

$testArgs = @(
    "test"
    $testProjectPath
    "-c", $Configuration
    "--no-build"
    "--no-restore"
    "--settings", $runSettingsPath
    "--logger", "console;verbosity=detailed"
    "--logger", "trx;LogFileName=UITestResults.trx"
    "--logger", "html;LogFileName=UITestResults.html"
)

if ($TestFilter) {
    $testArgs += "--filter", $TestFilter
}

if ($Verbose) {
    $testArgs += "--verbosity", "diagnostic"
}

Write-Host "Command: dotnet $($testArgs -join ' ')" -ForegroundColor Gray

try {
    & dotnet $testArgs
    $testResult = $LASTEXITCODE
    
    if ($testResult -eq 0) {
        Write-Host "`n✅ All UI tests passed!" -ForegroundColor Green
    } else {
        Write-Host "`n⚠️ Some tests failed or were skipped" -ForegroundColor Yellow
    }
} catch {
    Write-Host "`n❌ Error running tests: $_" -ForegroundColor Red
    $testResult = 1
} finally {
    # Clean up any remaining application processes
    Write-Host "`n🧹 Final cleanup..." -ForegroundColor Yellow
    $processes = Get-Process -Name "DailyTaskManager.Avalonia" -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Stop-Process -Force
        Write-Host "Cleaned up $($processes.Count) remaining processes"
    }
}

# Step 7: Display results location
$testResultsPath = Join-Path $scriptPath "TestResults"
if (Test-Path $testResultsPath) {
    Write-Host "`n📊 Test results available in: $testResultsPath" -ForegroundColor Cyan
    
    # List result files
    Get-ChildItem $testResultsPath -Filter "*.trx" | ForEach-Object {
        Write-Host "  📄 TRX Report: $($_.FullName)" -ForegroundColor Gray
    }
    
    Get-ChildItem $testResultsPath -Filter "*.html" | ForEach-Object {
        Write-Host "  📄 HTML Report: $($_.FullName)" -ForegroundColor Gray
    }
}

Write-Host "`n🏁 UI test run completed" -ForegroundColor Cyan
exit $testResult