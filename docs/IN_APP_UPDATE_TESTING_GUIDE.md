# In-App Update Testing Guide for Daily Task Manager

## 📱 NetSparkle Update System Overview

The Daily Task Manager uses **NetSparkle** for automatic update functionality. This guide explains how to test the update system thoroughly.

## ⚙️ Update Service Configuration

### Current Settings (appsettings.json)
```json
"UpdateService": {
  "AppCastUrl": "https://raw.githubusercontent.com/smartpointdevelopment/DailyTaskManager/master/installer/appcast.xml",
  "ApplicationName": "Daily Task Manager",
  "CompanyName": "Smart Point",
  "AutoUpdatesEnabled": true,
  "CheckInterval": "24:00:00"
}
```

### Key Components
- **AppCast URL**: RSS feed containing version information
- **Check Interval**: How often to check for updates (24 hours default)
- **Auto Updates**: Whether to check automatically on startup
- **UI Integration**: Avalonia-based update dialogs

## 🧪 Testing Scenarios

### Scenario 1: Fresh Installation Update Test

#### Prerequisites
1. **Install Previous Version** (v2.1.0 or earlier)
2. **Ensure Internet Connection**
3. **Create AppCast XML** with v2.2.0 information

#### Steps
1. **Launch Application** (v2.1.0)
2. **Login** with credentials (Username: Muhamed, Password: SelmaSumejaAnja123!*)
3. **Check Current Version** in Settings → Application Settings
4. **Trigger Manual Update Check** (if available in UI)
5. **Verify Update Dialog** appears with v2.2.0 information
6. **Accept Update** and wait for download
7. **Allow Restart** when prompted
8. **Verify New Version** shows v2.2.0
9. **Test New Features** (individual export paths in Settings)

### Scenario 2: Automatic Update Detection

#### Prerequisites
1. **Modify Check Interval** to 1 minute for testing:
   ```json
   "CheckInterval": "00:01:00"
   ```
2. **Restart Application** to apply new interval

#### Steps
1. **Launch Application**
2. **Login** and use normally
3. **Wait 1-2 minutes** for automatic check
4. **Look for Update Notification** (should appear automatically)
5. **Verify Background Check** doesn't interrupt usage
6. **Accept Update** when ready
7. **Confirm Smooth Update Process**

### Scenario 3: Network Failure Handling

#### Prerequisites
1. **Disable Internet Connection** temporarily
2. **Launch Application**

#### Steps
1. **Observe Startup Behavior** (should not hang or crash)
2. **Check for Error Messages** (should be minimal/none)
3. **Re-enable Internet**
4. **Wait for Next Check Cycle**
5. **Verify Update Detection** works after connectivity restored

### Scenario 4: Update Rejection/Postpone

#### Steps
1. **Trigger Update Check** (manual or automatic)
2. **When Update Dialog Appears**, click "Later" or "Cancel"
3. **Verify Application Continues** normally
4. **Check Update Offered Again** at next interval
5. **Confirm No Forced Updates** (user choice respected)

## 📋 AppCast XML Configuration

### Required AppCast Structure
Create `installer/appcast.xml` in your repository:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:sparkle="http://www.andymatuschak.org/xml-namespaces/sparkle">
    <channel>
        <title>Daily Task Manager Updates</title>
        <description>Update feed for Daily Task Manager</description>
        <language>en</language>
        <item>
            <title>Version 2.2.0 - Enhanced Settings Page</title>
            <sparkle:releaseNotesLink>https://github.com/MuhamedAlNaqli/DailyTaskManager/releases/tag/v2.2.0</sparkle:releaseNotesLink>
            <description><![CDATA[
                <h3>Major Settings Page Enhancements</h3>
                <ul>
                    <li><strong>Individual Export Paths:</strong> Separate paths for PDF, Word, and Excel files</li>
                    <li><strong>Enhanced Application Settings:</strong> Complete database connection details</li>
                    <li><strong>Streamlined Interface:</strong> Removed unnecessary Add/Save buttons</li>
                    <li><strong>Native Folder Picker:</strong> Modern Browse button functionality</li>
                </ul>
                <p>This update provides better control over export functionality and improved user experience.</p>
            ]]></description>
            <pubDate>Tue, 24 Sep 2025 19:00:00 +0000</pubDate>
            <enclosure url="https://github.com/MuhamedAlNaqli/DailyTaskManager/releases/download/v2.2.0/DailyTaskManager.Avalonia.exe"
                       sparkle:version="2.2.0"
                       sparkle:shortVersionString="2.2.0"
                       sparkle:minimumSystemVersion="10.0.0"
                       length="52428800"
                       type="application/octet-stream" />
        </item>
        <!-- Previous versions for rollback testing -->
        <item>
            <title>Version 2.1.0</title>
            <sparkle:releaseNotesLink>https://github.com/MuhamedAlNaqli/DailyTaskManager/releases/tag/v2.1.0</sparkle:releaseNotesLink>
            <pubDate>Mon, 23 Sep 2025 15:00:00 +0000</pubDate>
            <enclosure url="https://github.com/MuhamedAlNaqli/DailyTaskManager/releases/download/v2.1.0/DailyTaskManager.Avalonia.exe"
                       sparkle:version="2.1.0"
                       sparkle:shortVersionString="2.1.0"
                       length="52000000"
                       type="application/octet-stream" />
        </item>
    </channel>
</rss>
```

### Critical AppCast Requirements
1. **Correct Version Format**: Use semantic versioning (2.2.0)
2. **Valid Download URL**: Must be publicly accessible
3. **Accurate File Size**: Use actual file size in bytes
4. **Proper Date Format**: RFC 822 format for pubDate
5. **Release Notes**: Link to GitHub release page

## 🔍 Testing Checklist

### Before Testing
- [ ] **Build Release Version**: Ensure you have Release build of v2.2.0
- [ ] **Create GitHub Release**: Upload release assets
- [ ] **Update AppCast XML**: Point to new version
- [ ] **Verify URLs**: Ensure all links are accessible
- [ ] **Have Previous Version**: For upgrade testing

### Update Detection Tests
- [ ] **Manual Check Works**: User can manually trigger update check
- [ ] **Automatic Check Works**: Background checking functions
- [ ] **Version Comparison**: Correctly identifies newer versions
- [ ] **Network Resilience**: Handles network issues gracefully
- [ ] **Error Messaging**: Appropriate error messages for failures

### Update Process Tests
- [ ] **Download Progress**: Shows download progress to user
- [ ] **File Validation**: Verifies downloaded file integrity
- [ ] **Backup Creation**: Backs up current version (if implemented)
- [ ] **Installation Process**: Replaces executable correctly
- [ ] **Restart Handling**: Manages application restart properly

### Post-Update Verification
- [ ] **Version Updated**: Application reports new version
- [ ] **Features Work**: New settings features are functional
- [ ] **Data Preserved**: User data and settings maintained
- [ ] **Performance**: No degradation in performance
- [ ] **Stability**: No crashes or unexpected behavior

### User Experience Tests
- [ ] **Non-Intrusive**: Updates don't disrupt workflow
- [ ] **Clear Messaging**: Update dialogs are informative
- [ ] **User Choice**: Users can postpone or decline updates
- [ ] **Progress Feedback**: Clear indication of update progress
- [ ] **Error Recovery**: Graceful handling of update failures

## 🚨 Troubleshooting Common Issues

### Issue: Update Not Detected
**Symptoms**: Application doesn't offer update despite newer version available
**Solutions**:
1. **Check AppCast URL**: Verify URL is accessible from browser
2. **Verify Version Format**: Ensure version numbers follow semantic versioning
3. **Check Network**: Confirm internet connectivity
4. **Review Logs**: Check application logs for update service errors
5. **Clear Cache**: Remove any cached update information

### Issue: Download Fails
**Symptoms**: Update detected but download fails
**Solutions**:
1. **Verify Download URL**: Ensure release asset is publicly accessible
2. **Check File Size**: Verify file size matches AppCast specification
3. **Antivirus Interference**: Temporarily disable to test
4. **Firewall Issues**: Check firewall allows application internet access
5. **Disk Space**: Ensure sufficient disk space for download

### Issue: Update Doesn't Apply
**Symptoms**: Download completes but update doesn't apply
**Solutions**:
1. **Permissions**: Ensure application has write access to installation folder
2. **File Locks**: Check no other processes are using the application
3. **Digital Signature**: Verify executable signing (if implemented)
4. **SmartScreen**: Check Windows SmartScreen isn't blocking
5. **Manual Installation**: Test manual replacement of executable

### Issue: Application Won't Start After Update
**Symptoms**: Update completes but application fails to launch
**Solutions**:
1. **Compatibility**: Ensure new version compatible with system
2. **Dependencies**: Check all required libraries are present
3. **Configuration**: Verify configuration files are valid
4. **Rollback**: Restore previous version if available
5. **Clean Install**: Perform fresh installation

## 🎯 Success Metrics

### Functional Success
- ✅ **Update Detection**: New versions detected within check interval
- ✅ **Download Success**: Files download without corruption
- ✅ **Installation Success**: Updates apply correctly
- ✅ **Feature Verification**: New features work as expected
- ✅ **Data Preservation**: User settings and data maintained

### User Experience Success
- ✅ **Non-Disruptive**: Updates don't interrupt normal usage
- ✅ **Informative**: Clear information about update content
- ✅ **Reliable**: Consistent update process across different systems
- ✅ **Recoverable**: Graceful handling of failures
- ✅ **Performance**: No impact on application performance

## 📊 Testing Environment Setup

### Development Testing
```bash
# Build Release version for update testing
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" "D:\Internal Projects\DailyTaskManager\DailyTaskManager.Avalonia.sln" -p:Configuration=Release -verbosity:minimal

# Copy previous version for upgrade testing
cp "bin\Release\net8.0\win-x64\DailyTaskManager.Avalonia.exe" "DailyTaskManager-v2.1.0.exe"

# Test current version
"D:\Internal Projects\DailyTaskManager\src\DailyTaskManager.Avalonia\bin\Release\net8.0\win-x64\DailyTaskManager.Avalonia.exe"
```

### Production Testing
1. **Use Real GitHub Releases**: Test with actual GitHub release assets
2. **Real Network Conditions**: Test on various network conditions
3. **Multiple Systems**: Test on different Windows versions
4. **User Accounts**: Test with both admin and standard user accounts
5. **Real Usage Patterns**: Test during actual application usage

## 📝 Documentation Updates

After successful testing, update the following:
- [ ] **User Manual**: Document update process for end users
- [ ] **Release Notes**: Include update testing results
- [ ] **Troubleshooting Guide**: Add any issues encountered and solutions
- [ ] **System Requirements**: Update if new dependencies added
- [ ] **Installation Guide**: Include update-specific instructions

---

This comprehensive testing ensures the update system works reliably for end users and provides a smooth upgrade experience from v2.1.0 to v2.2.0.