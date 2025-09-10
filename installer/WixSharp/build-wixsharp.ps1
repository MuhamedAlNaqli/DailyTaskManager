# Daily Task Manager - WiX# Simple Build Script
param(
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host " DAILY TASK MANAGER - WIX# SIMPLE BUILD " -ForegroundColor White
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$ScriptRoot = $PSScriptRoot
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $ScriptRoot)
$AvaloniaProject = "$ProjectRoot\src\DailyTaskManager.Avalonia\DailyTaskManager.Avalonia.csproj"
$PublishPath = "$ProjectRoot\publish\win-x64"
$SetupScript = "$ScriptRoot\setup.cs"

Write-Host "Paths:" -ForegroundColor Yellow
Write-Host "   Setup Script: $SetupScript" -ForegroundColor Gray
Write-Host "   Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host "   Publish Path: $PublishPath" -ForegroundColor Gray
Write-Host ""

# Publish Avalonia application if needed
if (-not $SkipPublish) {
    Write-Host "Publishing Avalonia application..." -ForegroundColor Yellow
    
    & dotnet clean $AvaloniaProject -c Release --verbosity quiet
    & dotnet restore $AvaloniaProject --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Package restore failed!" -ForegroundColor Red
        exit 1
    }
    
    & dotnet publish $AvaloniaProject -c Release -r win-x64 --self-contained true --output $PublishPath /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   Avalonia application published successfully" -ForegroundColor Green
    Write-Host ""
}

# Check if WiX# is available via NuGet global tools
Write-Host "Attempting to build WiX# installer..." -ForegroundColor Yellow

# Try to use CScs (C# Script) if available 
try {
    Set-Location $ScriptRoot
    
    # Simple C# compilation approach using csc.exe directly
    Write-Host "   Compiling C# installer script..." -ForegroundColor Cyan
    
    # Try to find CSC
    $cscPath = ""
    $possiblePaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $cscPath = $path
            break
        }
    }
    
    if ([string]::IsNullOrEmpty($cscPath)) {
        Write-Host "   C# compiler (csc.exe) not found in Visual Studio paths" -ForegroundColor Red
        Write-Host "   Falling back to dotnet approach..." -ForegroundColor Yellow
        
        # Create a temporary project to run the setup
        $tempProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="WixSharp" Version="1.26.0" />
  </ItemGroup>
</Project>
"@
        $tempProject | Out-File -FilePath "TempInstaller.csproj" -Encoding UTF8
        
        # Copy the setup.cs as Program.cs for the temp project  
        Copy-Item "setup.cs" "Program.cs"
        
        # Build and run
        & dotnet restore "TempInstaller.csproj" --verbosity quiet
        if ($LASTEXITCODE -eq 0) {
            & dotnet run --project "TempInstaller.csproj" --verbosity quiet
        }
        
        # Cleanup
        Remove-Item "TempInstaller.csproj" -ErrorAction SilentlyContinue
        Remove-Item "Program.cs" -ErrorAction SilentlyContinue
        Remove-Item "bin" -Recurse -ErrorAction SilentlyContinue
        Remove-Item "obj" -Recurse -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "   Using Visual Studio C# compiler: $cscPath" -ForegroundColor Green
        Write-Host "   However, WiX# requires WiX toolset binaries which may not be available" -ForegroundColor Yellow
        Write-Host "   Recommend using the working traditional WiX approach instead" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "   WiX# build encountered issues: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "===============================================================" -ForegroundColor Yellow
Write-Host " BUILD SUMMARY " -ForegroundColor White
Write-Host "===============================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Note: WiX# requires WiX Toolset to be installed separately." -ForegroundColor Cyan
Write-Host "The traditional WiX XML approach is currently working and recommended." -ForegroundColor Cyan
Write-Host ""