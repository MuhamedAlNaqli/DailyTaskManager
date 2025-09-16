# Daily Task Manager - Rollback Management Script
# This script manages version rollback capabilities and safety checks

param(
    [Parameter(Mandatory=$false)]
    [string]$Action = "info",
    
    [Parameter(Mandatory=$false)]
    [string]$TargetVersion = "",
    
    [switch]$Force,
    [switch]$CreateBackup,
    [switch]$RestoreSettings,
    [switch]$DryRun,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Registry paths
$AppRegistryPath = "HKLM:\SOFTWARE\SmartPoint\DailyTaskManager"
$UserRegistryPath = "HKCU:\SOFTWARE\SmartPoint\DailyTaskManager"
$BackupRegistryPath = "HKLM:\SOFTWARE\SmartPoint\DailyTaskManager\Backup"
$VersionHistoryPath = "HKLM:\SOFTWARE\SmartPoint\DailyTaskManager\VersionHistory"

# Application paths
$DefaultInstallPath = "${env:ProgramFiles}\SmartPoint\DailyTaskManager"
$BackupPath = "${env:ProgramData}\SmartPoint\DailyTaskManager\Backups"
$SettingsPath = "${env:APPDATA}\SmartPoint\DailyTaskManager"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "INFO"     { "White" }
        "SUCCESS"  { "Green" }
        "WARNING"  { "Yellow" }
        "ERROR"    { "Red" }
        "CRITICAL" { "Magenta" }
        default    { "Gray" }
    }
    
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-AdminPrivileges {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-CurrentVersion {
    try {
        if (Test-Path $AppRegistryPath) {
            $regValue = Get-ItemProperty -Path $AppRegistryPath -Name "Version" -ErrorAction SilentlyContinue
            if ($regValue) {
                return $regValue.Version
            }
        }
        return $null
    }
    catch {
        Write-Log "Failed to read current version: $($_.Exception.Message)" "ERROR"
        return $null
    }
}

function Get-InstallPath {
    try {
        if (Test-Path $AppRegistryPath) {
            $regValue = Get-ItemProperty -Path $AppRegistryPath -Name "InstallPath" -ErrorAction SilentlyContinue
            if ($regValue) {
                return $regValue.InstallPath
            }
        }
        return $DefaultInstallPath
    }
    catch {
        return $DefaultInstallPath
    }
}

function Get-VersionHistory {
    try {
        $history = @()
        
        if (Test-Path $VersionHistoryPath) {
            $versions = Get-ChildItem -Path $VersionHistoryPath -Name
            
            foreach ($version in $versions) {
                $versionPath = "$VersionHistoryPath\$version"
                $props = Get-ItemProperty -Path $versionPath -ErrorAction SilentlyContinue
                
                if ($props) {
                    $history += [PSCustomObject]@{
                        Version = $version
                        InstallDate = $props.InstallDate
                        PreviousVersion = $props.PreviousVersion
                        InstallMethod = $props.InstallMethod
                        BackupAvailable = $props.BackupAvailable -eq "True"
                        Channel = $props.Channel
                        RollbackSafe = $props.RollbackSafe -eq "True"
                    }
                }
            }
        }
        
        return $history | Sort-Object { [Version]$_.Version } -Descending
    }
    catch {
        Write-Log "Failed to read version history: $($_.Exception.Message)" "ERROR"
        return @()
    }
}

function Add-VersionHistory {
    param(
        [string]$Version,
        [string]$PreviousVersion,
        [string]$InstallMethod,
        [bool]$BackupCreated,
        [string]$Channel
    )
    
    try {
        # Create version history registry structure
        if (-not (Test-Path $VersionHistoryPath)) {
            New-Item -Path $VersionHistoryPath -Force | Out-Null
        }
        
        $versionEntryPath = "$VersionHistoryPath\$Version"
        if (-not (Test-Path $versionEntryPath)) {
            New-Item -Path $versionEntryPath -Force | Out-Null
        }
        
        # Store version information
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Set-ItemProperty -Path $versionEntryPath -Name "InstallDate" -Value $timestamp
        Set-ItemProperty -Path $versionEntryPath -Name "PreviousVersion" -Value $PreviousVersion
        Set-ItemProperty -Path $versionEntryPath -Name "InstallMethod" -Value $InstallMethod
        Set-ItemProperty -Path $versionEntryPath -Name "BackupAvailable" -Value $BackupCreated.ToString()
        Set-ItemProperty -Path $versionEntryPath -Name "Channel" -Value $Channel
        Set-ItemProperty -Path $versionEntryPath -Name "RollbackSafe" -Value "True"
        
        Write-Log "Added version $Version to history" "SUCCESS"
    }
    catch {
        Write-Log "Failed to add version history: $($_.Exception.Message)" "ERROR"
    }
}

function Test-RollbackSafety {
    param([string]$TargetVersion, [string]$CurrentVersion)
    
    Write-Log "Performing rollback safety checks..." "INFO"
    
    $warnings = @()
    $criticalIssues = @()
    
    # Version comparison
    try {
        $currentVer = [Version]$CurrentVersion
        $targetVer = [Version]$TargetVersion
        
        if ($targetVer -gt $currentVer) {
            $criticalIssues += "Target version ($TargetVersion) is newer than current version ($CurrentVersion)"
        }
        
        $versionDiff = $currentVer.Major - $targetVer.Major
        if ($versionDiff -gt 1) {
            $warnings += "Major version downgrade detected (may cause compatibility issues)"
        }
        
    }
    catch {
        $warnings += "Could not parse version numbers for comparison"
    }
    
    # Check if target version exists in history
    $history = Get-VersionHistory
    $targetEntry = $history | Where-Object { $_.Version -eq $TargetVersion }
    
    if (-not $targetEntry) {
        $criticalIssues += "Target version $TargetVersion was never installed on this system"
    } elseif (-not $targetEntry.RollbackSafe) {
        $criticalIssues += "Target version $TargetVersion is marked as not safe for rollback"
    } elseif (-not $targetEntry.BackupAvailable) {
        $warnings += "No backup available for target version $TargetVersion"
    }
    
    # Check running processes
    $runningProcesses = Get-Process -Name "DailyTaskManager*" -ErrorAction SilentlyContinue
    if ($runningProcesses) {
        $warnings += "Daily Task Manager is currently running (will need to be closed)"
    }
    
    # Check disk space
    $installPath = Get-InstallPath
    $drive = Split-Path -Qualifier $installPath
    $freeSpace = (Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$drive'").FreeSpace / 1GB
    if ($freeSpace -lt 1) {
        $warnings += "Low disk space on install drive (less than 1 GB free)"
    }
    
    # Database compatibility check (simplified)
    $dbPath = "$SettingsPath\dailytasks.db"
    if (Test-Path $dbPath) {
        $warnings += "User data exists - ensure target version is compatible with current database schema"
    }
    
    return @{
        IsSafe = $criticalIssues.Count -eq 0
        Warnings = $warnings
        CriticalIssues = $criticalIssues
        TargetVersionInfo = $targetEntry
    }
}

function Invoke-Rollback {
    param([string]$TargetVersion)
    
    if (-not (Test-AdminPrivileges)) {
        throw "Administrator privileges required for rollback operation"
    }
    
    $currentVersion = Get-CurrentVersion
    Write-Log "Initiating rollback from $currentVersion to $TargetVersion" "INFO"
    
    # Safety check
    $safetyCheck = Test-RollbackSafety -TargetVersion $TargetVersion -CurrentVersion $currentVersion
    
    if (-not $safetyCheck.IsSafe -and -not $Force) {
        Write-Log "Rollback safety check failed:" "CRITICAL"
        foreach ($issue in $safetyCheck.CriticalIssues) {
            Write-Log "  • $issue" "ERROR"
        }
        Write-Log "Use -Force to proceed anyway (NOT RECOMMENDED)" "WARNING"
        return $false
    }
    
    # Show warnings
    if ($safetyCheck.Warnings.Count -gt 0) {
        Write-Log "Rollback warnings:" "WARNING"
        foreach ($warning in $safetyCheck.Warnings) {
            Write-Log "  • $warning" "WARNING"
        }
        
        if (-not $Force -and -not $DryRun) {
            $response = Read-Host "Continue with rollback? (y/N)"
            if ($response -notlike "y*") {
                Write-Log "Rollback cancelled by user" "INFO"
                return $false
            }
        }
    }
    
    if ($DryRun) {
        Write-Log "DRY RUN: Rollback simulation completed successfully" "SUCCESS"
        return $true
    }
    
    try {
        # Step 1: Stop application
        Write-Log "Stopping Daily Task Manager processes..." "INFO"
        Get-Process -Name "DailyTaskManager*" -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep -Seconds 2
        
        # Step 2: Create backup of current installation
        if ($CreateBackup) {
            Write-Log "Creating backup of current installation..." "INFO"
            $backupDir = "$BackupPath\$currentVersion-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            $installPath = Get-InstallPath
            
            if (Test-Path $installPath) {
                New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
                Copy-Item -Path "$installPath\*" -Destination $backupDir -Recurse -Force
                Write-Log "Backup created at: $backupDir" "SUCCESS"
            }
        }
        
        # Step 3: Find and install target version
        $targetInstaller = "$BackupPath\$TargetVersion\DailyTaskManager-v$TargetVersion-win-x64.msi"
        
        if (-not (Test-Path $targetInstaller)) {
            throw "Target version installer not found: $targetInstaller"
        }
        
        Write-Log "Installing target version: $TargetVersion" "INFO"
        $installArgs = @("/i", "`"$targetInstaller`"", "/qn", "REINSTALL=ALL", "REINSTALLMODE=amus")
        Start-Process -FilePath "msiexec.exe" -ArgumentList $installArgs -Wait -Verb RunAs
        
        # Step 4: Update version tracking
        Write-Log "Updating version tracking..." "INFO"
        Set-ItemProperty -Path $AppRegistryPath -Name "Version" -Value $TargetVersion
        Set-ItemProperty -Path $AppRegistryPath -Name "RolledBackFrom" -Value $currentVersion
        Set-ItemProperty -Path $AppRegistryPath -Name "RollbackDate" -Value (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
        
        # Step 5: Restore settings if requested
        if ($RestoreSettings -and $safetyCheck.TargetVersionInfo.BackupAvailable) {
            Write-Log "Restoring user settings..." "INFO"
            $settingsBackup = "$BackupPath\$TargetVersion\Settings"
            if (Test-Path $settingsBackup) {
                Copy-Item -Path "$settingsBackup\*" -Destination $SettingsPath -Recurse -Force
                Write-Log "Settings restored" "SUCCESS"
            }
        }
        
        Write-Log "Rollback completed successfully!" "SUCCESS"
        Write-Log "Rolled back from $currentVersion to $TargetVersion" "SUCCESS"
        
        return $true
    }
    catch {
        Write-Log "Rollback failed: $($_.Exception.Message)" "CRITICAL"
        return $false
    }
}

function Show-RollbackInfo {
    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host " DAILY TASK MANAGER - ROLLBACK MANAGER " -ForegroundColor White
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host ""
    
    $currentVersion = Get-CurrentVersion
    Write-Host "Current Version: $currentVersion" -ForegroundColor Yellow
    
    $history = Get-VersionHistory
    
    if ($history.Count -eq 0) {
        Write-Host "No version history available." -ForegroundColor Red
        Write-Host "Install the application using the MSI installer to track version history." -ForegroundColor Gray
        return
    }
    
    Write-Host ""
    Write-Host "Version History:" -ForegroundColor Yellow
    Write-Host "=================" -ForegroundColor Yellow
    
    foreach ($entry in $history) {
        $marker = if ($entry.Version -eq $currentVersion) { "→ CURRENT" } else { "" }
        $backupStatus = if ($entry.BackupAvailable) { "✓" } else { "✗" }
        $rollbackSafe = if ($entry.RollbackSafe) { "✓" } else { "✗" }
        
        Write-Host ""
        Write-Host "  Version: $($entry.Version) $marker" -ForegroundColor $(if ($entry.Version -eq $currentVersion) { "Green" } else { "White" })
        Write-Host "    Installed: $($entry.InstallDate)" -ForegroundColor Gray
        Write-Host "    Channel: $($entry.Channel)" -ForegroundColor Gray
        Write-Host "    Previous: $($entry.PreviousVersion)" -ForegroundColor Gray
        Write-Host "    Method: $($entry.InstallMethod)" -ForegroundColor Gray
        Write-Host "    Backup Available: $backupStatus" -ForegroundColor $(if ($entry.BackupAvailable) { "Green" } else { "Red" })
        Write-Host "    Rollback Safe: $rollbackSafe" -ForegroundColor $(if ($entry.RollbackSafe) { "Green" } else { "Red" })
    }
    
    Write-Host ""
    Write-Host "Rollback Commands:" -ForegroundColor Yellow
    Write-Host "==================" -ForegroundColor Yellow
    
    $rollbackCandidates = $history | Where-Object { 
        $_.Version -ne $currentVersion -and 
        $_.RollbackSafe -and 
        [Version]$_.Version -lt [Version]$currentVersion 
    } | Select-Object -First 3
    
    if ($rollbackCandidates.Count -gt 0) {
        foreach ($candidate in $rollbackCandidates) {
            Write-Host "  Rollback to $($candidate.Version): .\RollbackManager.ps1 -Action rollback -TargetVersion $($candidate.Version)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "  No safe rollback targets available." -ForegroundColor Red
    }
    
    Write-Host ""
}

# Main execution
Write-Host ""
Write-Host "Daily Task Manager - Rollback Manager" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

if (-not (Test-AdminPrivileges) -and $Action -eq "rollback") {
    Write-Log "Administrator privileges required for rollback operations" "ERROR"
    Write-Log "Please run as administrator" "ERROR"
    exit 1
}

try {
    switch ($Action.ToLower()) {
        "info" {
            Show-RollbackInfo
        }
        
        "check" {
            if ([string]::IsNullOrEmpty($TargetVersion)) {
                Write-Log "TargetVersion parameter is required for 'check' action" "ERROR"
                exit 1
            }
            
            $currentVersion = Get-CurrentVersion
            $safetyCheck = Test-RollbackSafety -TargetVersion $TargetVersion -CurrentVersion $currentVersion
            
            Write-Host ""
            Write-Host "Rollback Safety Check Results:" -ForegroundColor Yellow
            Write-Host "==============================" -ForegroundColor Yellow
            Write-Host "Target Version: $TargetVersion" -ForegroundColor White
            Write-Host "Current Version: $currentVersion" -ForegroundColor White
            Write-Host "Overall Safety: $(if ($safetyCheck.IsSafe) { 'SAFE' } else { 'UNSAFE' })" -ForegroundColor $(if ($safetyCheck.IsSafe) { "Green" } else { "Red" })
            
            if ($safetyCheck.CriticalIssues.Count -gt 0) {
                Write-Host ""
                Write-Host "Critical Issues:" -ForegroundColor Red
                foreach ($issue in $safetyCheck.CriticalIssues) {
                    Write-Host "  • $issue" -ForegroundColor Red
                }
            }
            
            if ($safetyCheck.Warnings.Count -gt 0) {
                Write-Host ""
                Write-Host "Warnings:" -ForegroundColor Yellow
                foreach ($warning in $safetyCheck.Warnings) {
                    Write-Host "  • $warning" -ForegroundColor Yellow
                }
            }
        }
        
        "rollback" {
            if ([string]::IsNullOrEmpty($TargetVersion)) {
                Write-Log "TargetVersion parameter is required for 'rollback' action" "ERROR"
                exit 1
            }
            
            $success = Invoke-Rollback -TargetVersion $TargetVersion
            if (-not $success) {
                exit 1
            }
        }
        
        "history" {
            $history = Get-VersionHistory
            Write-Host ""
            Write-Host "Version History (JSON):" -ForegroundColor Yellow
            $history | ConvertTo-Json -Depth 2 | Write-Host
        }
        
        default {
            Write-Log "Unknown action: $Action" "ERROR"
            Write-Log "Available actions: info, check, rollback, history" "INFO"
            exit 1
        }
    }
}
catch {
    Write-Log "Script execution failed: $($_.Exception.Message)" "ERROR"
    exit 1
}

Write-Host ""