//css_dir ..\..\..
//css_ref WixSharp.dll
//css_ref System.Core.dll

using System;
using System.IO;
using System.Windows.Forms;
using WixSharp;

class Script
{
    static public void Main()
    {
        // Get paths
        var currentDir = Directory.GetCurrentDirectory();
        var projectRoot = Path.GetFullPath(Path.Combine(currentDir, "..", ".."));  
        var publishDir = Path.Combine(projectRoot, "publish", "win-x64");
        var outputDir = Path.Combine(projectRoot, "installer", "output");

        Console.WriteLine($"Project Root: {projectRoot}");
        Console.WriteLine($"Publish Dir: {publishDir}");
        Console.WriteLine($"Output Dir: {outputDir}");

        // Ensure output directory exists
        Directory.CreateDirectory(outputDir);

        // Check if published files exist
        var mainExe = Path.Combine(publishDir, "DailyTaskManager.Avalonia.exe");
        var appSettings = Path.Combine(publishDir, "appsettings.json");
        
        if (!File.Exists(mainExe))
        {
            Console.WriteLine("Error: Published executable not found!");
            Console.WriteLine("Please run the Avalonia application build first.");
            return;
        }

        // Create the project
        var project = new Project("Daily Task Manager",
            new Dir(@"%ProgramFiles%\SmartPoint\DailyTaskManager",
                new File(mainExe,
                    new FileShortcut("Daily Task Manager", @"%ProgramMenu%\Daily Task Manager"),
                    new FileShortcut("Daily Task Manager", @"%Desktop%")),
                new Files(Path.Combine(publishDir, "*.json")))
        );

        // Set project properties
        project.GUID = new Guid("A7B2C3D4-E5F6-7890-ABCD-123456789012");
        project.Version = new Version("2.0.0.0");
        project.Platform = Platform.x64;
        project.InstallScope = InstallScope.perMachine;
        project.Description = "Daily Task Manager - Cross-platform task management application";

        // Control panel info
        project.ControlPanelInfo.Manufacturer = "Smart Point";
        project.ControlPanelInfo.HelpLink = "https://github.com/smartpointdevelopment/DailyTaskManager";
        project.ControlPanelInfo.UrlInfoAbout = "https://github.com/smartpointdevelopment/DailyTaskManager";
        project.ControlPanelInfo.NoRepair = true;
        project.ControlPanelInfo.NoModify = true;

        // Add uninstall shortcut in start menu folder
        project.Dirs = project.Dirs.Add(
            new Dir(@"%ProgramMenu%\Daily Task Manager",
                new ExeFileShortcut("Uninstall Daily Task Manager", "[System64Folder]msiexec.exe", "/x [ProductCode]")
            ));

        // Launch conditions
        project.LaunchConditions.Add(new LaunchCondition("VersionNT>=601", 
            "This application requires Windows 7 or higher."));

        // Major upgrade
        project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;

        // Build the MSI
        var msiPath = Path.Combine(outputDir, "DailyTaskManager-v2.0.0.0-win-x64.msi");
        
        Console.WriteLine("Building MSI installer using WiX#...");
        
        try
        {
            Compiler.BuildMsi(project, msiPath);
            
            Console.WriteLine($"✅ MSI installer created successfully: {msiPath}");
            
            // Get file info
            var fileInfo = new FileInfo(msiPath);
            var sizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
            
            Console.WriteLine($"📁 Size: {sizeMB} MB");
            Console.WriteLine($"📅 Created: {fileInfo.CreationTime}");
            
            Console.WriteLine("🎉 WiX# installation package is ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error building MSI: {ex.Message}");
        }
    }
}