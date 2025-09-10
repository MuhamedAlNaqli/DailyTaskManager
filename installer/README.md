# Daily Task Manager - Windows Installer

This directory contains the Windows MSI installer configuration for the Daily Task Manager application.

## Files

- **`installer.wxs`** - WiX v4 installer configuration (main file)
- **`build-installer.ps1`** - PowerShell script to build the MSI installer  
- **`license.rtf`** - Software license agreement
- **`output/`** - Generated MSI installer and documentation
- **`WixSharp/`** - Alternative WiX# approach (experimental)

## Quick Build

```powershell
# Build installer (includes app publish + MSI creation)
.\build-installer.ps1

# Build installer without republishing app  
.\build-installer.ps1 -SkipBuild -SkipPublish

# Clean build from scratch
.\build-installer.ps1 -Clean
```

## Requirements

- **WiX Toolset v4.0.5+** (installed globally via `dotnet tool install --global wix`)
- **.NET 8 SDK** for publishing the Avalonia application
- **Windows 7+** (target compatibility)

## Generated Installer Features

- **Installation Path**: `C:\Program Files\SmartPoint\DailyTaskManager\`
- **Start Menu Shortcuts**: Launch app + Uninstall shortcut
- **Desktop Shortcut**: Optional (user can select during install)
- **Add/Remove Programs**: Full Windows integration
- **Self-contained**: No additional runtime requirements
- **File Size**: ~38MB (includes .NET runtime)

## Installation Process

1. **GUI Installation**: Double-click the generated `.msi` file
2. **Silent Installation**: `msiexec /i "installer.msi" /quiet`  
3. **Administrator Rights**: Required for Program Files installation

## Output Files

After running the build script, check the `output/` directory:

- `DailyTaskManager-v2.0.0.0-win-x64.msi` - Main installer
- `INSTALLATION_GUIDE.txt` - End-user instructions
- `Install-DailyTaskManager.ps1` - PowerShell installation script

## Troubleshooting

**Build Errors:**
- Ensure WiX Toolset v4 is installed: `wix --version`
- Verify .NET 8 SDK: `dotnet --version`
- Check all file paths in `installer.wxs` are correct

**Installation Errors:**  
- Run MSI as Administrator
- Check Windows Installer service is running
- Ensure target drive has sufficient space (~200MB)

## Development Notes

The installer uses **WiX v4 Package syntax** (not the older Product syntax). Key components:

- **Package Definition**: Application metadata and version
- **Feature Structure**: Main app + optional desktop shortcut  
- **Component Groups**: Files, shortcuts, and registry entries
- **Launch Conditions**: Windows version compatibility check
- **Major Upgrade**: Automatic upgrade handling

For future enhancements, the **WixSharp/** directory contains a C#-based alternative approach that may be easier to maintain for complex scenarios.