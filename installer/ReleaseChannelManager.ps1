# Daily Task Manager - Release Channel Management Script
# This script helps manage different release channels (stable, beta, alpha)

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("stable", "beta", "alpha", "development")]
    [string]$Channel,
    
    [Parameter(Mandatory=$false)]
    [string]$Action = "set",
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Registry paths
$AppRegistryPath = "HKLM:\SOFTWARE\SmartPoint\DailyTaskManager"
$UserRegistryPath = "HKCU:\SOFTWARE\SmartPoint\DailyTaskManager"

# AppCast URLs for different channels
$AppCastUrls = @{
    "stable"      = "https://raw.githubusercontent.com/smartpointdevelopment/DailyTaskManager/master/installer/appcast.xml"
    "beta"        = "https://raw.githubusercontent.com/smartpointdevelopment/DailyTaskManager/beta/installer/appcast-beta.xml"
    "alpha"       = "https://raw.githubusercontent.com/smartpointdevelopment/DailyTaskManager/alpha/installer/appcast-alpha.xml"
    "development" = "https://raw.githubusercontent.com/smartpointdevelopment/DailyTaskManager/develop/installer/appcast-dev.xml"
}

# Channel descriptions
$ChannelDescriptions = @{
    "stable"      = "Stable release channel - fully tested and recommended for production use"
    "beta"        = "Beta release channel - pre-release versions with new features, may have bugs"
    "alpha"       = "Alpha release channel - early development versions, expect issues"
    "development" = "Development channel - bleeding edge builds, for developers only"
}

# Channel update intervals (in hours)
$UpdateIntervals = @{
    "stable"      = 24   # Check daily
    "beta"        = 12   # Check twice daily
    "alpha"       = 6    # Check every 6 hours
    "development" = 2    # Check every 2 hours
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "INFO"    { "White" }
        "SUCCESS" { "Green" }
        "WARNING" { "Yellow" }
        "ERROR"   { "Red" }
        default   { "Gray" }
    }
    
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-AdminPrivileges {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-CurrentChannel {
    try {
        if (Test-Path $AppRegistryPath) {
            $regValue = Get-ItemProperty -Path $AppRegistryPath -Name "ReleaseChannel" -ErrorAction SilentlyContinue
            if ($regValue) {
                return $regValue.ReleaseChannel
            }
        }
        
        # Fallback to user registry
        if (Test-Path $UserRegistryPath) {
            $regValue = Get-ItemProperty -Path $UserRegistryPath -Name "ReleaseChannel" -ErrorAction SilentlyContinue
            if ($regValue) {
                return $regValue.ReleaseChannel
            }
        }
        
        return "stable" # Default
    }
    catch {
        Write-Log "Failed to read current channel: $($_.Exception.Message)" "ERROR"
        return "stable"
    }
}

function Get-CurrentVersion {
    try {
        if (Test-Path $AppRegistryPath) {
            $regValue = Get-ItemProperty -Path $AppRegistryPath -Name "Version" -ErrorAction SilentlyContinue
            if ($regValue) {
                return $regValue.Version
            }
        }
        return "Unknown"
    }
    catch {
        Write-Log "Failed to read current version: $($_.Exception.Message)" "ERROR"
        return "Unknown"
    }
}

function Set-ReleaseChannel {
    param([string]$NewChannel)
    
    try {
        $currentChannel = Get-CurrentChannel
        $currentVersion = Get-CurrentVersion
        
        Write-Log "Current Channel: $currentChannel" "INFO"
        Write-Log "Current Version: $currentVersion" "INFO"
        Write-Log "Target Channel: $NewChannel" "INFO"
        
        if ($currentChannel -eq $NewChannel -and -not $Force) {
            Write-Log "Already on $NewChannel channel. Use -Force to reconfigure." "WARNING"
            return
        }
        
        # Create registry keys if they don't exist
        if (-not (Test-Path $AppRegistryPath)) {
            New-Item -Path $AppRegistryPath -Force | Out-Null
            Write-Log "Created application registry key" "SUCCESS"
        }
        
        if (-not (Test-Path $UserRegistryPath)) {
            New-Item -Path $UserRegistryPath -Force | Out-Null
            Write-Log "Created user registry key" "SUCCESS"
        }
        
        # Set release channel
        Set-ItemProperty -Path $AppRegistryPath -Name "ReleaseChannel" -Value $NewChannel
        Set-ItemProperty -Path $UserRegistryPath -Name "ReleaseChannel" -Value $NewChannel
        
        # Set AppCast URL
        $appCastUrl = $AppCastUrls[$NewChannel]
        Set-ItemProperty -Path $AppRegistryPath -Name "AppCastUrl" -Value $appCastUrl
        Set-ItemProperty -Path $UserRegistryPath -Name "AppCastUrl" -Value $appCastUrl
        
        # Set update interval
        $updateInterval = $UpdateIntervals[$NewChannel]
        Set-ItemProperty -Path $AppRegistryPath -Name "UpdateInterval" -Value $updateInterval
        Set-ItemProperty -Path $UserRegistryPath -Name "UpdateInterval" -Value $updateInterval
        
        # Set last channel change timestamp
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Set-ItemProperty -Path $AppRegistryPath -Name "ChannelChangedDate" -Value $timestamp
        Set-ItemProperty -Path $UserRegistryPath -Name "ChannelChangedDate" -Value $timestamp
        
        Write-Log "Successfully switched to $NewChannel channel" "SUCCESS"
        Write-Log "AppCast URL: $appCastUrl" "INFO"
        Write-Log "Update Interval: $updateInterval hours" "INFO"
        Write-Log "Description: $($ChannelDescriptions[$NewChannel])" "INFO"
        
        # Show warning for non-stable channels
        if ($NewChannel -ne "stable") {
            Write-Log "" "INFO"
            Write-Log "⚠️  WARNING: You are now on the $NewChannel channel" "WARNING"
            Write-Log "   This channel may contain unstable or experimental features." "WARNING"
            Write-Log "   Switch back to 'stable' channel for production use." "WARNING"
        }
        
    }
    catch {
        Write-Log "Failed to set release channel: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Get-ChannelInfo {
    $currentChannel = Get-CurrentChannel
    $currentVersion = Get-CurrentVersion
    
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host " DAILY TASK MANAGER - RELEASE CHANNEL INFO " -ForegroundColor White
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Current Configuration:" -ForegroundColor Yellow
    Write-Host "   Channel: $currentChannel" -ForegroundColor White
    Write-Host "   Version: $currentVersion" -ForegroundColor White
    Write-Host "   Description: $($ChannelDescriptions[$currentChannel])" -ForegroundColor Gray
    
    if (Test-Path $AppRegistryPath) {
        try {
            $appCastUrl = Get-ItemProperty -Path $AppRegistryPath -Name "AppCastUrl" -ErrorAction SilentlyContinue
            $updateInterval = Get-ItemProperty -Path $AppRegistryPath -Name "UpdateInterval" -ErrorAction SilentlyContinue
            $lastChanged = Get-ItemProperty -Path $AppRegistryPath -Name "ChannelChangedDate" -ErrorAction SilentlyContinue
            
            if ($appCastUrl) {
                Write-Host "   AppCast URL: $($appCastUrl.AppCastUrl)" -ForegroundColor Gray
            }
            if ($updateInterval) {
                Write-Host "   Update Check Interval: $($updateInterval.UpdateInterval) hours" -ForegroundColor Gray
            }
            if ($lastChanged) {
                Write-Host "   Channel Last Changed: $($lastChanged.ChannelChangedDate)" -ForegroundColor Gray
            }
        }
        catch {
            # Ignore errors reading additional properties
        }
    }
    
    Write-Host ""
    Write-Host "Available Channels:" -ForegroundColor Yellow
    foreach ($ch in $ChannelDescriptions.Keys | Sort-Object) {
        $marker = if ($ch -eq $currentChannel) { "→" } else { " " }
        Write-Host "  $marker $ch - $($ChannelDescriptions[$ch])" -ForegroundColor $(if ($ch -eq $currentChannel) { "Green" } else { "White" })
    }
    
    Write-Host ""
}

function Reset-ChannelConfig {
    Write-Log "Resetting to stable channel configuration..." "INFO"
    Set-ReleaseChannel -NewChannel "stable"
}

# Main execution
Write-Host ""
Write-Host "Daily Task Manager - Release Channel Manager" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Check admin privileges for system-wide changes
if (-not (Test-AdminPrivileges)) {
    Write-Log "⚠️  Running without administrator privileges" "WARNING"
    Write-Log "   Some operations may require elevated permissions" "WARNING"
}

try {
    switch ($Action.ToLower()) {
        "set" {
            if ([string]::IsNullOrEmpty($Channel)) {
                Write-Log "Channel parameter is required for 'set' action" "ERROR"
                exit 1
            }
            Set-ReleaseChannel -NewChannel $Channel
        }
        
        "get" {
            Get-ChannelInfo
        }
        
        "info" {
            Get-ChannelInfo
        }
        
        "reset" {
            Reset-ChannelConfig
        }
        
        default {
            Write-Log "Unknown action: $Action" "ERROR"
            Write-Log "Available actions: set, get, info, reset" "INFO"
            exit 1
        }
    }
}
catch {
    Write-Log "Script execution failed: $($_.Exception.Message)" "ERROR"
    exit 1
}

Write-Host ""
Write-Host "Operation completed successfully!" -ForegroundColor Green
Write-Host ""

# Usage examples
if ($Action -eq "set" -and $Channel -eq "stable") {
    Write-Host "💡 Usage Examples:" -ForegroundColor Yellow
    Write-Host "   Switch to beta channel:  .\ReleaseChannelManager.ps1 -Channel beta -Action set" -ForegroundColor Gray
    Write-Host "   View current channel:     .\ReleaseChannelManager.ps1 -Action info" -ForegroundColor Gray
    Write-Host "   Reset to stable:          .\ReleaseChannelManager.ps1 -Action reset" -ForegroundColor Gray
    Write-Host ""
}