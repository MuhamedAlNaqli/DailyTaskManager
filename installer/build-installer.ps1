# Daily Task Manager - Professional MSI Installer Build Script
# PowerShell script to build professional MSI installer using WiX Toolset v4

param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Version = "",
    [switch]$Clean,
    [switch]$SkipBuild,
    [switch]$SkipPublish,
    [switch]$ValidateOnly,
    [switch]$Verbose
)

# Set error handling
$ErrorActionPreference = "Stop"

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host " DAILY TASK MANAGER - MSI INSTALLER BUILD " -ForegroundColor White
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$ScriptRoot = $PSScriptRoot
$ProjectRoot = Split-Path -Parent $ScriptRoot
$ProjectPath = "$ProjectRoot\src\DailyTaskManager.Avalonia\DailyTaskManager.Avalonia.csproj"
$PublishPath = "$ProjectRoot\publish\$RuntimeIdentifier"
$InstallerPath = "$ProjectRoot\installer"
$OutputPath = "$InstallerPath\output"
$WxsFile = "$InstallerPath\installer.wxs"

# Auto-detect version from GitVersion if not provided
if ([string]::IsNullOrEmpty($Version)) {
    Write-Host "Auto-detecting version using GitVersion..." -ForegroundColor Yellow
    try {
        $gitVersionOutput = & dotnet-gitversion --root $ProjectRoot 2>&1
        if ($LASTEXITCODE -eq 0) {
            $gitVersion = $gitVersionOutput | ConvertFrom-Json
            $Version = $gitVersion.AssemblySemFileVer
            Write-Host "   Detected version: $Version" -ForegroundColor Green
        } else {
            Write-Host "   GitVersion failed, using fallback version: 2.1.0.0" -ForegroundColor Yellow
            $Version = "2.1.0.0"
        }
    } catch {
        Write-Host "   GitVersion not available, using fallback version: 2.1.0.0" -ForegroundColor Yellow
        $Version = "2.1.0.0"
    }
    Write-Host ""
}

$MsiOutput = "$OutputPath\DailyTaskManager-v$Version-$RuntimeIdentifier.msi"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "   Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host "   Publish Path: $PublishPath" -ForegroundColor Gray
Write-Host "   Output Path: $OutputPath" -ForegroundColor Gray
Write-Host "   MSI Output: $MsiOutput" -ForegroundColor Gray
Write-Host ""

# Verify prerequisites
Write-Host "Verifying prerequisites..." -ForegroundColor Yellow

# Check if WiX is installed
try {
    $wixVersion = & wix --version 2>&1
    Write-Host "   WiX Toolset: $wixVersion" -ForegroundColor Green
} catch {
    Write-Host "   WiX Toolset not found!" -ForegroundColor Red
    Write-Host "   Install with: dotnet tool install --global wix" -ForegroundColor Yellow
    exit 1
}

# Check if .NET is available
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "   .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "   .NET SDK not found!" -ForegroundColor Red
    exit 1
}

# Check if project file exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "   Project file not found: $ProjectPath" -ForegroundColor Red
    exit 1
}
Write-Host "   Project file found" -ForegroundColor Green

# Check if WiX source file exists
if (-not (Test-Path $WxsFile)) {
    Write-Host "   WiX source file not found: $WxsFile" -ForegroundColor Red
    exit 1
}
Write-Host "   WiX source file found" -ForegroundColor Green
Write-Host ""

# Clean previous builds if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    
    if (Test-Path $PublishPath) {
        Remove-Item -Recurse -Force $PublishPath
        Write-Host "   Cleaned publish directory" -ForegroundColor Green
    }
    
    if (Test-Path $OutputPath) {
        Get-ChildItem -Path $OutputPath -Filter "*.msi" -ErrorAction SilentlyContinue | Remove-Item -Force
        Get-ChildItem -Path $OutputPath -Filter "*.wixobj" -ErrorAction SilentlyContinue | Remove-Item -Force
        Get-ChildItem -Path $OutputPath -Filter "*.wixpdb" -ErrorAction SilentlyContinue | Remove-Item -Force
        Write-Host "   Cleaned installer output directory" -ForegroundColor Green
    }
    Write-Host ""
}

# Create directories
Write-Host "Preparing directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $PublishPath | Out-Null
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
Write-Host "   Directories prepared" -ForegroundColor Green
Write-Host ""

# Build and publish the application
if (-not $SkipBuild -and -not $SkipPublish) {
    Write-Host "Building and publishing application..." -ForegroundColor Yellow
    
    # Clean and restore
    Write-Host "   Restoring packages..." -ForegroundColor Cyan
    & dotnet clean $ProjectPath -c $Configuration --verbosity quiet
    & dotnet restore $ProjectPath --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Package restore failed!" -ForegroundColor Red
        exit 1
    }
    
    # Build first to check for compilation errors
    Write-Host "   Building project..." -ForegroundColor Cyan
    & dotnet build $ProjectPath -c $Configuration --no-restore --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Build failed!" -ForegroundColor Red
        exit 1
    }
    
    # Publish self-contained
    Write-Host "   Publishing self-contained executable..." -ForegroundColor Cyan
    $publishArgs = @(
        "publish"
        $ProjectPath
        "-c", $Configuration
        "-r", $RuntimeIdentifier
        "--self-contained", "true"
        "--output", $PublishPath
        "/p:PublishSingleFile=true"
        "/p:IncludeNativeLibrariesForSelfExtract=true"
        "/p:PublishTrimmed=false"
        "/p:DebugType=embedded"
        "--verbosity", $(if ($Verbose) { "normal" } else { "quiet" })
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   Application built and published successfully" -ForegroundColor Green
    
    # Display published files
    $publishedFiles = Get-ChildItem -Path $PublishPath
    Write-Host "   Published files:" -ForegroundColor Cyan
    foreach ($file in $publishedFiles) {
        $sizeKB = [math]::Round($file.Length / 1KB, 1)
        Write-Host "      $($file.Name) ($sizeKB KB)" -ForegroundColor Gray
    }
    Write-Host ""
}

# Verify required files exist
Write-Host "Verifying published files..." -ForegroundColor Yellow
$requiredFiles = @(
    "DailyTaskManager.Avalonia.exe",
    "appsettings.json"
)

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $PublishPath $file
    if (-not (Test-Path $filePath)) {
        Write-Host "   Required file missing: $file" -ForegroundColor Red
        exit 1
    }
    Write-Host "   $file" -ForegroundColor Green
}
Write-Host ""

# Exit early if validation only
if ($ValidateOnly) {
    Write-Host "Validation completed successfully!" -ForegroundColor Green
    exit 0
}

# Create license file if it doesn't exist
$licenseFile = "$InstallerPath\license.rtf"
if (-not (Test-Path $licenseFile)) {
    Write-Host "Creating license file..." -ForegroundColor Yellow
    $licenseContent = "{\rtf1\ansi\deff0 {\fonttbl {\f0 Times New Roman;}}`n\f0\fs24`nDaily Task Manager - Software License Agreement`n`nThis software is provided `"as is`" without warranty of any kind.`n`nCopyright (c) 2024 Smart Point Development. All rights reserved.`n`nPermission is hereby granted to use this software for personal and commercial purposes.`n`nFor support and updates, visit: https://github.com/smartpointdevelopment/DailyTaskManager`n}"
    $licenseContent | Out-File -FilePath $licenseFile -Encoding ASCII
    Write-Host "   License file created" -ForegroundColor Green
    Write-Host ""
}

# Build MSI installer
Write-Host "Building MSI installer..." -ForegroundColor Yellow

# Set WiX variables
$wixVariables = @(
    "-d", "PublishDir=$PublishPath",
    "-d", "ProjectDir=$InstallerPath",
    "-d", "Version=$Version"
)

# Build command
$buildArgs = @(
    "build"
    $WxsFile
    "-out", $MsiOutput
    "-arch", "x64"
    "-dcl", "high"
) + $wixVariables

if ($Verbose) {
    Write-Host "   WiX Build Command:" -ForegroundColor Cyan
    Write-Host "      wix $($buildArgs -join ' ')" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "   Compiling installer..." -ForegroundColor Cyan
& wix @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "   MSI build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "   1. Verify all files exist in publish directory" -ForegroundColor White
    Write-Host "   2. Check WiX source file syntax" -ForegroundColor White
    Write-Host "   3. Ensure no files are locked by running processes" -ForegroundColor White
    Write-Host "   4. Run with -Verbose flag for detailed output" -ForegroundColor White
    exit 1
}

Write-Host "   MSI installer built successfully" -ForegroundColor Green
Write-Host ""

# Verify MSI file
if (-not (Test-Path $MsiOutput)) {
    Write-Host "MSI file was not created!" -ForegroundColor Red
    exit 1
}

$msiFileInfo = Get-Item $MsiOutput
$msiSizeMB = [math]::Round($msiFileInfo.Length / 1MB, 2)

Write-Host "MSI Installer Information:" -ForegroundColor Yellow
Write-Host "   File: $($msiFileInfo.Name)" -ForegroundColor Cyan
Write-Host "   Size: $msiSizeMB MB" -ForegroundColor Cyan
Write-Host "   Path: $MsiOutput" -ForegroundColor Cyan
Write-Host "   Created: $($msiFileInfo.CreationTime)" -ForegroundColor Cyan
Write-Host ""

# Create installation script
$installScript = "$OutputPath\Install-DailyTaskManager.ps1"
$installScriptContent = @"
# Daily Task Manager - Installation Script
param([switch]`$Quiet)

`$MsiPath = "`$PSScriptRoot\DailyTaskManager-v$Version-$RuntimeIdentifier.msi"

if (-not (Test-Path `$MsiPath)) {
    Write-Host "MSI file not found: `$MsiPath" -ForegroundColor Red
    exit 1
}

Write-Host "Installing Daily Task Manager..." -ForegroundColor Green

`$Args = @("/i", "`$MsiPath")
if (`$Quiet) {
    `$Args += "/quiet"
} else {
    `$Args += "/passive"
}

Start-Process -FilePath "msiexec.exe" -ArgumentList `$Args -Wait -Verb RunAs
Write-Host "Installation completed!" -ForegroundColor Green
"@

$installScriptContent | Out-File -FilePath $installScript -Encoding UTF8
Write-Host "Created installation script: $installScript" -ForegroundColor Cyan

# Final summary
Write-Host ""
Write-Host "===============================================================" -ForegroundColor Green
Write-Host " BUILD COMPLETED SUCCESSFULLY! " -ForegroundColor White
Write-Host "===============================================================" -ForegroundColor Green
Write-Host ""

Write-Host "MSI installer created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Installer Details:" -ForegroundColor Yellow
Write-Host "   Product: Daily Task Manager v$Version" -ForegroundColor White
Write-Host "   Platform: $RuntimeIdentifier" -ForegroundColor White
Write-Host "   Size: $msiSizeMB MB" -ForegroundColor White
Write-Host "   File: $MsiOutput" -ForegroundColor White
Write-Host ""
Write-Host "Installation Options:" -ForegroundColor Yellow
Write-Host "   1. Double-click the MSI file for GUI installation" -ForegroundColor White
Write-Host "   2. Run Install-DailyTaskManager.ps1 for scripted installation" -ForegroundColor White
Write-Host "   3. Command line: msiexec /i `"$($msiFileInfo.Name)`"" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Test the installer on a clean system" -ForegroundColor White
Write-Host "   2. Verify all shortcuts work correctly" -ForegroundColor White
Write-Host "   3. Test uninstallation process" -ForegroundColor White
Write-Host "   4. Distribute the MSI file to users" -ForegroundColor White
Write-Host ""

# Open output directory
try {
    Start-Process -FilePath "explorer.exe" -ArgumentList $OutputPath
} catch {
    Write-Host "Output directory: $OutputPath" -ForegroundColor Cyan
}