using System;
using System.IO;
using WixSharp;
using WixSharp.CommonTasks;

namespace DailyTaskManagerInstaller;

class Program
{
    static void Main()
    {
        // Get paths
        var currentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var projectRoot = Path.GetFullPath(Path.Combine(currentDir!, "..", "..", ".."));
        var publishDir = Path.Combine(projectRoot, "publish", "win-x64");
        var outputDir = Path.Combine(projectRoot, "installer", "output");

        Console.WriteLine($"Project Root: {projectRoot}");
        Console.WriteLine($"Publish Dir: {publishDir}");
        Console.WriteLine($"Output Dir: {outputDir}");

        // Ensure output directory exists
        Directory.CreateDirectory(outputDir);

        // Create the project
        var project = new Project("Daily Task Manager")
        {
            GUID = new Guid("A7B2C3D4-E5F6-7890-ABCD-123456789012"),
            Version = new Version("2.0.0.0"),
            Platform = Platform.x64,
            InstallScope = InstallScope.perMachine,
            Description = "Daily Task Manager - Cross-platform task management application",
            ControlPanelInfo = new ProductInfo
            {
                Manufacturer = "Smart Point",
                HelpLink = "https://github.com/smartpointdevelopment/DailyTaskManager",
                UrlInfoAbout = "https://github.com/smartpointdevelopment/DailyTaskManager",
                ProductIcon = Path.Combine(publishDir, "DailyTaskManager.Avalonia.exe"),
                NoRepair = true,
                NoModify = true
            }
        };

        // Define installation directory
        project.InstallDir = @"%ProgramFiles%\SmartPoint\DailyTaskManager";

        // Application files
        project.AddDir(new Dir(project.InstallDir,
            new File(Path.Combine(publishDir, "DailyTaskManager.Avalonia.exe"))
            {
                Shortcuts = new[]
                {
                    new FileShortcut("Daily Task Manager", @"%ProgramMenu%\Daily Task Manager")
                    {
                        Description = "Daily Task Manager - Modern task management application"
                    },
                    new FileShortcut("Daily Task Manager", @"%Desktop%")
                    {
                        Description = "Daily Task Manager - Modern task management application"
                    }
                }
            },
            new File(Path.Combine(publishDir, "appsettings.json")),
            new File(Path.Combine(publishDir, "appsettings.Development.json"))
        ));

        // Start Menu folder with uninstall shortcut
        project.AddDir(new Dir(@"%ProgramMenu%\Daily Task Manager",
            new ExeFileShortcut("Uninstall Daily Task Manager", "[System64Folder]msiexec.exe", "/x [ProductCode]")
        ));

        // Set UI options
        project.UI = WUI.WixUI_InstallDir;

        // Launch conditions
        project.LaunchConditions.Add(new LaunchCondition("VersionNT>=601", 
            "This application requires Windows 7 or higher."));

        // Major upgrade
        project.MajorUpgrade = MajorUpgrade.Default;
        project.MajorUpgrade.Schedule = UpgradeSchedule.afterInstallInitialize;

        // Build the MSI
        var msiPath = Path.Combine(outputDir, "DailyTaskManager-v2.0.0.0-win-x64.msi");
        
        Console.WriteLine("Building MSI installer...");
        project.BuildMsi(msiPath);
        
        Console.WriteLine($"✅ MSI installer created successfully: {msiPath}");
        
        // Get file info
        var fileInfo = new FileInfo(msiPath);
        var sizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
        
        Console.WriteLine($"📁 Size: {sizeMB} MB");
        Console.WriteLine($"📅 Created: {fileInfo.CreationTime}");
        
        // Create installation guide
        CreateInstallationGuide(outputDir, fileInfo.Name, sizeMB);
        
        Console.WriteLine("🎉 Installation package is ready!");
    }

    static void CreateInstallationGuide(string outputDir, string msiFileName, double sizeMB)
    {
        var guide = $@"=================================================================
   DAILY TASK MANAGER - INSTALLATION GUIDE
=================================================================

Thank you for downloading Daily Task Manager!

SYSTEM REQUIREMENTS:
- Windows 7 or later (64-bit)
- No additional software required (self-contained)

QUICK INSTALLATION:
1. Right-click ""{msiFileName}""
2. Select ""Run as administrator""
3. Follow the installation wizard
4. Choose installation options:
   - Application files (required)
   - Start Menu shortcuts (recommended)  
   - Desktop shortcut (optional)
5. Click Install and wait for completion
6. Launch Daily Task Manager from Start Menu

INSTALLATION LOCATION:
Default: C:\Program Files\SmartPoint\DailyTaskManager
Note: Requires administrator privileges for installation

FEATURES INCLUDED:
✓ Task management with due dates and priorities
✓ Google OAuth authentication
✓ Export to Excel functionality
✓ Light/Dark theme support
✓ Cross-platform compatibility

SHORTCUTS CREATED:
- Start Menu: ""Daily Task Manager"" folder
  - Daily Task Manager (launch app)
  - Uninstall Daily Task Manager
- Desktop: Daily Task Manager (if selected)

FIRST RUN SETUP:
1. The application will prompt for database setup
2. Configure PostgreSQL connection or use default settings
3. Create your first user account or sign in with Google
4. Start managing your tasks!

UNINSTALLATION:
- Use Windows Settings → Apps
- Or Control Panel → Programs and Features
- Or Start Menu → Daily Task Manager → Uninstall

COMMAND LINE INSTALLATION:
For silent installation:
msiexec /i ""{msiFileName}"" /quiet

For interactive installation:
msiexec /i ""{msiFileName}"" /passive

TROUBLESHOOTING:
- Run as Administrator if installation fails
- Ensure Windows Installer service is running
- Check available disk space (200MB required)
- For detailed logs: msiexec /i installer.msi /l*v install.log

SUPPORT:
GitHub: https://github.com/smartpointdevelopment/DailyTaskManager
Contact: Smart Point Development

VERSION: 2.0.0.0
BUILD DATE: {DateTime.Now:MMMM dd, yyyy}
INSTALLER SIZE: {sizeMB} MB

=================================================================";

        File.WriteAllText(Path.Combine(outputDir, "INSTALLATION_GUIDE.txt"), guide);
    }
}