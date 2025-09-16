# 🎯 **Daily Task Manager - Complete Versioning & Update System Workflow**

**Document Version**: 1.0  
**Last Updated**: September 11, 2025  
**System Status**: Production Ready - All 4 Phases Complete

---

## 📋 **Complete Workflow: Start to Finish**

### **Phase 1: Development & Version Management**

#### **1.1 Developer Makes Changes**
```bash
# Developer works on features
git add .
git commit -m "feat: add new task filtering feature"
git push origin feature/task-filtering
```

#### **1.2 Automatic Version Generation**
- **GitVersion** automatically calculates next version based on commits
- **Current setup**: Uses mainline mode with semantic versioning
- **Version format**: `2.1.0`, `2.2.0-beta.1`, `2.3.0-alpha.5`

```bash
# Check version locally
dotnet-gitversion
# Output: 2.2.0-feature-task-filtering.1+5
```

---

### **Phase 2: Build & Release Process**

#### **2.1 Create Release (Manual or Automated)**

**Option A: Manual Release**
```bash
# Create release tag
git tag v2.2.0
git push origin v2.2.0
```

**Option B: GitHub Actions Manual Trigger**
- Go to GitHub Actions → "Build and Release" workflow
- Click "Run workflow"
- Select:
  - **Release type**: `minor` (for 2.1.x → 2.2.x)
  - **Release channel**: `stable` (or `beta`, `alpha`, `development`)
  - **Create release**: `true`

#### **2.2 Automated CI/CD Pipeline Execution**

**GitHub Actions automatically:**

1. **Builds Application**
   ```bash
   dotnet build DailyTaskManager.Avalonia.sln --configuration Release
   dotnet publish --runtime win-x64 --self-contained true
   ```

2. **Generates Version** (via GitVersion)
   - Determines version: `2.2.0`
   - Sets assembly versions consistently

3. **Creates MSI Installer**
   ```bash
   wix build installer.wxs -out DailyTaskManager-v2.2.0-win-x64.msi
   ```

4. **Creates GitHub Release**
   - Uploads MSI file as release asset
   - Generates professional release notes
   - Tags release with version

5. **Updates AppCast XML**
   ```xml
   <item>
       <title>Daily Task Manager 2.2.0</title>
       <sparkle:version>2.2.0.0</sparkle:version>
       <link>https://github.com/.../DailyTaskManager-v2.2.0-win-x64.msi</link>
   </item>
   ```

---

### **Phase 3: User Update Experience**

#### **3.1 Automatic Update Detection**

**For existing users running v2.1.0:**

1. **NetSparkleUpdater** runs automatically (every 24 hours for stable channel)
2. **Checks AppCast** at: `https://raw.githubusercontent.com/.../appcast.xml`
3. **Finds newer version** (2.2.0 vs current 2.1.0)
4. **Shows update notification** with Avalonia UI

#### **3.2 User Update Process**

**User sees update dialog:**
```
┌─────────────────────────────────────────┐
│ Daily Task Manager Update Available     │
├─────────────────────────────────────────┤
│ Version 2.2.0 is now available!        │
│ You have version 2.1.0                 │
│                                         │
│ What's New:                             │
│ • New task filtering feature            │
│ • Performance improvements              │
│ • Bug fixes                             │
│                                         │
│    [Skip]  [Install Later]  [Install]  │
└─────────────────────────────────────────┘
```

**When user clicks "Install":**
1. **Downloads** new MSI installer automatically
2. **Closes** application gracefully
3. **Runs installer** with upgrade parameters: `/i DailyTaskManager-v2.2.0-win-x64.msi /quiet REINSTALL=ALL REINSTALLMODE=amus`
4. **WiX handles upgrade**: 
   - Removes old version files
   - Installs new version files
   - Preserves user settings and data
   - Updates registry entries
5. **Restarts application** automatically

---

### **Phase 4: Advanced Management Features**

#### **4.1 Release Channel Management**

**Switching to Beta Channel:**
```powershell
# Run as Administrator
.\installer\ReleaseChannelManager.ps1 -Channel beta -Action set

# Output:
# ✅ Successfully switched to beta channel
# AppCast URL: https://raw.githubusercontent.com/.../appcast-beta.xml
# Update Interval: 12 hours
# ⚠️  WARNING: You are now on the beta channel
```

**Result:** User now receives beta updates (e.g., 2.3.0-beta.1) more frequently

#### **4.2 Rollback Capability**

**If user has issues with v2.2.0:**

```powershell
# Check rollback options
.\installer\RollbackManager.ps1 -Action info

# Output shows version history:
# → CURRENT Version: 2.2.0 (Installed: 2025-09-11 10:30:00)
#   Version: 2.1.0 (Installed: 2025-08-15 14:20:00) ✓ Rollback Safe
#   Version: 2.0.5 (Installed: 2025-07-20 09:15:00) ✓ Rollback Safe

# Rollback to previous version
.\installer\RollbackManager.ps1 -Action rollback -TargetVersion 2.1.0

# Performs safety checks, then:
# ✅ Rollback completed successfully!
# Rolled back from 2.2.0 to 2.1.0
```

---

### **Phase 5: Different Release Scenarios**

#### **5.1 Stable Release Process**
```bash
git checkout main
git tag v2.2.0
git push origin v2.2.0
# → Triggers stable release for all stable channel users
```

#### **5.2 Beta Release Process**  
```bash
git checkout develop
git tag v2.3.0-beta.1
git push origin v2.3.0-beta.1
# → GitHub Actions with release_channel: beta
# → Updates appcast-beta.xml
# → Only beta channel users receive this update
```

#### **5.3 Hotfix Release Process**
```bash
git checkout main
git cherry-pick <hotfix-commit>
git tag v2.2.1
git push origin v2.2.1
# → Emergency patch release
# → All users get critical update
```

---

## 🔄 **Complete User Journey Examples**

### **Scenario A: New User Installation**
1. **Downloads** `DailyTaskManager-v2.2.0-win-x64.msi` from GitHub releases
2. **Runs installer** → installs to `C:\Program Files\SmartPoint\DailyTaskManager\`
3. **Registry entries created**:
   - Version: 2.2.0
   - Channel: stable
   - UpdatesEnabled: true
4. **Application starts** → NetSparkleUpdater initialized
5. **Future updates** happen automatically

### **Scenario B: Existing User Update**
1. **App checks for updates** (daily for stable users)
2. **Finds v2.3.0** available
3. **Shows notification** → user approves
4. **Downloads and installs** → seamless upgrade
5. **App restarts** with new version
6. **Version history updated** in registry

### **Scenario C: Beta Tester Workflow**  
1. **User switches** to beta channel with PowerShell tool
2. **Receives v2.4.0-beta.1** (12-hour check interval)
3. **Tests new features** → finds critical bug
4. **Uses rollback tool** → returns to stable v2.3.0
5. **Reports bug** → developer fixes
6. **Receives v2.4.0-beta.2** with fix

### **Scenario D: Enterprise Deployment**
1. **IT Admin** downloads MSI from GitHub releases
2. **Tests in staging** with rollback capabilities
3. **Deploys via Group Policy** or SCCM
4. **Users get updates** through normal channels
5. **Emergency rollback** available if needed

---

## 📊 **System Architecture Overview**

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Developer     │───▶│  GitHub Actions  │───▶│  GitHub Release │
│   Commits       │    │     CI/CD        │    │   + MSI File    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │                        │
                                ▼                        ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │   AppCast XML    │    │  User's PC      │
                       │     Updated      │◀───│ NetSparkle     │
                       └──────────────────┘    │ Checks Updates  │
                                              └─────────────────┘
                                                       │
                                              ┌─────────────────┐
                                              │ Automatic MSI   │
                                              │ Install/Update  │
                                              └─────────────────┘
```

---

## 🛠️ **Management Tools Reference**

### **Release Channel Manager**

**Location**: `installer/ReleaseChannelManager.ps1`

**Usage Examples:**
```powershell
# View current channel info
.\ReleaseChannelManager.ps1 -Action info

# Switch to beta channel
.\ReleaseChannelManager.ps1 -Channel beta -Action set

# Reset to stable channel
.\ReleaseChannelManager.ps1 -Action reset
```

**Available Channels:**
- **`stable`**: Production releases, checked daily
- **`beta`**: Pre-release versions, checked every 12 hours  
- **`alpha`**: Early development, checked every 6 hours
- **`development`**: Bleeding edge, checked every 2 hours

### **Rollback Manager**

**Location**: `installer/RollbackManager.ps1`

**Usage Examples:**
```powershell
# View version history and rollback options
.\RollbackManager.ps1 -Action info

# Check if rollback to specific version is safe
.\RollbackManager.ps1 -Action check -TargetVersion 2.1.0

# Perform rollback (requires admin privileges)
.\RollbackManager.ps1 -Action rollback -TargetVersion 2.1.0

# Dry run to test rollback process
.\RollbackManager.ps1 -Action rollback -TargetVersion 2.1.0 -DryRun
```

### **Installer Build Script**

**Location**: `installer/build-installer.ps1`

**Usage Examples:**
```powershell
# Build installer with auto-detected version
.\build-installer.ps1

# Build with specific version
.\build-installer.ps1 -Version 2.2.0.0

# Clean build with custom configuration
.\build-installer.ps1 -Configuration Release -Clean

# Validate without building installer
.\build-installer.ps1 -ValidateOnly
```

---

## 🔧 **Technical Configuration**

### **AppCast URLs by Channel**
- **Stable**: `https://raw.githubusercontent.com/.../master/installer/appcast.xml`
- **Beta**: `https://raw.githubusercontent.com/.../beta/installer/appcast-beta.xml`
- **Alpha**: `https://raw.githubusercontent.com/.../alpha/installer/appcast-alpha.xml`  
- **Development**: `https://raw.githubusercontent.com/.../develop/installer/appcast-dev.xml`

### **Registry Locations**
- **System**: `HKLM\SOFTWARE\SmartPoint\DailyTaskManager`
- **User**: `HKCU\SOFTWARE\SmartPoint\DailyTaskManager`
- **Version History**: `HKLM\SOFTWARE\SmartPoint\DailyTaskManager\VersionHistory`

### **File Locations**
- **Installation**: `C:\Program Files\SmartPoint\DailyTaskManager\`
- **User Data**: `%APPDATA%\SmartPoint\DailyTaskManager\`
- **Backups**: `%PROGRAMDATA%\SmartPoint\DailyTaskManager\Backups\`

---

## ⚠️ **Important Considerations**

### **For Developers**

1. **Commit Message Format**: Use conventional commits for automatic version bumping
   ```bash
   feat: new feature (+minor version)
   fix: bug fix (+patch version)  
   BREAKING CHANGE: breaking change (+major version)
   ```

2. **Testing**: Always test installer upgrades on clean Windows VMs

3. **Release Tags**: Use semantic versioning format (`v2.1.0`, `v2.2.0-beta.1`)

### **For IT Administrators**

1. **Deployment**: MSI files support silent installation via Group Policy
   ```cmd
   msiexec /i DailyTaskManager-v2.2.0-win-x64.msi /quiet
   ```

2. **Network Deployment**: Consider hosting AppCast files internally for enterprise environments

3. **Rollback Planning**: Test rollback procedures in staging before production deployment

### **For End Users**

1. **Channel Selection**: Stable channel recommended for production use
2. **Update Notifications**: Updates check automatically but require user approval
3. **Rollback**: Available through PowerShell tools if issues occur
4. **Data Safety**: User data and settings preserved during updates

---

## 🎯 **Key Benefits Achieved**

1. **🔄 Fully Automated**: From code commit to user update
2. **🛡️ Safe Updates**: Rollback capabilities with safety checks
3. **🎛️ Flexible Channels**: Different update tracks for different users
4. **📦 Professional Installer**: MSI with proper Windows integration
5. **🔍 Complete Tracking**: Version history and installation metadata
6. **⚡ Fast Deployment**: Minutes from release tag to user availability
7. **🛠️ Enterprise Ready**: PowerShell tools for IT management

---

## 📞 **Support & Troubleshooting**

### **Common Issues**

**Update Check Fails:**
- Check internet connection and AppCast URL accessibility
- Verify channel configuration with `ReleaseChannelManager.ps1 -Action info`

**Installer Upgrade Issues:**
- Ensure Daily Task Manager is closed before upgrade
- Run installer as Administrator if needed
- Check Windows Event Viewer for MSI error details

**Rollback Problems:**
- Verify administrator privileges
- Check version history with `RollbackManager.ps1 -Action info`
- Ensure target version was previously installed

### **Getting Help**

- **Documentation**: See `VERSIONING_AND_UPDATES_ROADMAP.md` for implementation details
- **Issues**: Report problems at GitHub repository issue tracker
- **Tools**: All PowerShell tools include built-in help and error messages

---

**The system now provides a complete, professional-grade software distribution and update experience comparable to commercial software products!**