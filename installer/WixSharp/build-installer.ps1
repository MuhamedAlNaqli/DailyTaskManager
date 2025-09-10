# Daily Task Manager - WiX# Installer Build Script
param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [switch]$Clean,
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host " DAILY TASK MANAGER - WIX# INSTALLER BUILD " -ForegroundColor White
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$ScriptRoot = $PSScriptRoot
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $ScriptRoot)
$AvaloniaProject = "$ProjectRoot\src\DailyTaskManager.Avalonia\DailyTaskManager.Avalonia.csproj"
$InstallerProject = "$ScriptRoot\DailyTaskManagerInstaller.csproj"
$PublishPath = "$ProjectRoot\publish\$RuntimeIdentifier"
$OutputPath = "$ProjectRoot\installer\output"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "   Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host "   Avalonia Project: $AvaloniaProject" -ForegroundColor Gray
Write-Host "   Installer Project: $InstallerProject" -ForegroundColor Gray
Write-Host "   Publish Path: $PublishPath" -ForegroundColor Gray
Write-Host "   Output Path: $OutputPath" -ForegroundColor Gray
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $PublishPath) {
        Remove-Item -Recurse -Force $PublishPath
        Write-Host "   Cleaned publish directory" -ForegroundColor Green
    }
    if (Test-Path $OutputPath) {
        Get-ChildItem -Path $OutputPath -Filter "*.msi" -ErrorAction SilentlyContinue | Remove-Item -Force
        Write-Host "   Cleaned installer output directory" -ForegroundColor Green
    }
    Write-Host ""
}

# Publish Avalonia application if needed
if (-not $SkipPublish) {
    Write-Host "Publishing Avalonia application..." -ForegroundColor Yellow
    
    & dotnet clean $AvaloniaProject -c $Configuration --verbosity quiet
    & dotnet restore $AvaloniaProject --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Package restore failed!" -ForegroundColor Red
        exit 1
    }
    
    & dotnet publish $AvaloniaProject -c $Configuration -r $RuntimeIdentifier --self-contained true --output $PublishPath /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   Avalonia application published successfully" -ForegroundColor Green
    Write-Host ""
}

# Build and run WiX# installer
Write-Host "Building WiX# installer..." -ForegroundColor Yellow

& dotnet restore $InstallerProject --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   WiX# project restore failed!" -ForegroundColor Red
    exit 1
}

& dotnet run --project $InstallerProject --configuration $Configuration --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "   WiX# installer build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "===============================================================" -ForegroundColor Green
Write-Host " BUILD COMPLETED SUCCESSFULLY! " -ForegroundColor White  
Write-Host "===============================================================" -ForegroundColor Green
Write-Host ""

# Open output directory
try {
    Start-Process -FilePath "explorer.exe" -ArgumentList $OutputPath
} catch {
    Write-Host "Output directory: $OutputPath" -ForegroundColor Cyan
}