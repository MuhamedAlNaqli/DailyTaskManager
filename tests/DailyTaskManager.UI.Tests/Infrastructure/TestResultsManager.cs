using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web;

namespace DailyTaskManager.UI.Tests.Infrastructure;

/// <summary>
/// Manages test results directory structure and screenshot organization
/// </summary>
public class TestResultsManager
{
    private readonly string _baseTestResultsPath;
    private readonly string _currentTestRunPath;
    private readonly string _screenshotsPath;
    private readonly string _comparisonsPath;
    private readonly string _baselinePath;
    
    public TestResultsManager(TestContext? testContext = null)
    {
        // Base path for all test results
        _baseTestResultsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "..", "..", "..", "TestResults");

        // Current test run directory with timestamp
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var testRunId = testContext?.Properties["TestRunId"]?.ToString() ?? Guid.NewGuid().ToString()[..8];
        _currentTestRunPath = Path.Combine(_baseTestResultsPath, $"TestRun_{timestamp}_{testRunId}");
        
        // Subdirectories for organized storage
        _screenshotsPath = Path.Combine(_currentTestRunPath, "Screenshots");
        _comparisonsPath = Path.Combine(_currentTestRunPath, "Comparisons");
        _baselinePath = Path.Combine(_baseTestResultsPath, "Baselines");
        
        // Ensure directories exist
        Directory.CreateDirectory(_screenshotsPath);
        Directory.CreateDirectory(_comparisonsPath);
        Directory.CreateDirectory(_baselinePath);
        
        Console.WriteLine($"📁 Test results will be saved to: {_currentTestRunPath}");
    }

    /// <summary>
    /// Gets the path for a screenshot file for the current test
    /// </summary>
    public string GetScreenshotPath(string testName, string stepDescription, string extension = "png")
    {
        var fileName = ScreenshotHelper.GenerateScreenshotFileName(testName, stepDescription, extension);
        return Path.Combine(_screenshotsPath, fileName);
    }

    /// <summary>
    /// Gets the path for a screenshot file for a specific test class
    /// </summary>
    public string GetTestClassScreenshotPath(string testClassName, string testName, string stepDescription, string extension = "png")
    {
        var testClassFolder = Path.Combine(_screenshotsPath, SanitizeDirectoryName(testClassName));
        Directory.CreateDirectory(testClassFolder);
        
        var fileName = ScreenshotHelper.GenerateScreenshotFileName(testName, stepDescription, extension);
        return Path.Combine(testClassFolder, fileName);
    }

    /// <summary>
    /// Gets the path for a comparison result file
    /// </summary>
    public string GetComparisonPath(string testName, string comparisonType, string extension = "png")
    {
        var fileName = ScreenshotHelper.GenerateScreenshotFileName(testName, comparisonType, extension);
        return Path.Combine(_comparisonsPath, fileName);
    }

    /// <summary>
    /// Gets the path for a baseline screenshot
    /// </summary>
    public string GetBaselinePath(string testName, string stepDescription, string extension = "png")
    {
        var fileName = $"{SanitizeDirectoryName(testName)}_{SanitizeDirectoryName(stepDescription)}.{extension}";
        return Path.Combine(_baselinePath, fileName);
    }

    /// <summary>
    /// Creates a test-specific subdirectory and returns the path
    /// </summary>
    public string CreateTestDirectory(string testName)
    {
        var testDir = Path.Combine(_screenshotsPath, SanitizeDirectoryName(testName));
        Directory.CreateDirectory(testDir);
        return testDir;
    }

    /// <summary>
    /// Saves test metadata to a JSON file
    /// </summary>
    public void SaveTestMetadata(string testName, TestMetadata metadata)
    {
        try
        {
            var metadataPath = Path.Combine(_currentTestRunPath, $"{SanitizeDirectoryName(testName)}_metadata.json");
            var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(metadataPath, json);
            Console.WriteLine($"📝 Test metadata saved: {metadataPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to save test metadata: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads test metadata from a JSON file
    /// </summary>
    public TestMetadata? LoadTestMetadata(string testName)
    {
        try
        {
            var metadataPath = Path.Combine(_currentTestRunPath, $"{SanitizeDirectoryName(testName)}_metadata.json");
            if (File.Exists(metadataPath))
            {
                var json = File.ReadAllText(metadataPath);
                return System.Text.Json.JsonSerializer.Deserialize<TestMetadata>(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to load test metadata: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Creates an HTML test report with embedded screenshots
    /// </summary>
    public void GenerateTestReport(List<TestResultSummary> testResults)
    {
        try
        {
            var reportPath = Path.Combine(_currentTestRunPath, "TestReport.html");
            var html = GenerateHtmlReport(testResults);
            File.WriteAllText(reportPath, html);
            Console.WriteLine($"📊 Test report generated: {reportPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to generate test report: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up old test result directories (keeps last N runs)
    /// </summary>
    public void CleanupOldResults(int keepLastNRuns = 10)
    {
        try
        {
            var testRunDirs = Directory.GetDirectories(_baseTestResultsPath, "TestRun_*")
                .Select(d => new DirectoryInfo(d))
                .OrderByDescending(d => d.CreationTime)
                .Skip(keepLastNRuns)
                .ToList();

            foreach (var dir in testRunDirs)
            {
                try
                {
                    dir.Delete(true);
                    Console.WriteLine($"🧹 Cleaned up old test results: {dir.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to delete {dir.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error during cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all screenshot files for a specific test
    /// </summary>
    public List<string> GetTestScreenshots(string testName)
    {
        var screenshots = new List<string>();
        
        try
        {
            var testDir = Path.Combine(_screenshotsPath, SanitizeDirectoryName(testName));
            if (Directory.Exists(testDir))
            {
                screenshots.AddRange(Directory.GetFiles(testDir, "*.png"));
                screenshots.AddRange(Directory.GetFiles(testDir, "*.jpg"));
            }
            
            // Also check root screenshots folder for files containing test name
            var rootScreenshots = Directory.GetFiles(_screenshotsPath, "*.png")
                .Concat(Directory.GetFiles(_screenshotsPath, "*.jpg"))
                .Where(f => Path.GetFileName(f).Contains(SanitizeDirectoryName(testName)))
                .ToList();
            
            screenshots.AddRange(rootScreenshots);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error getting test screenshots: {ex.Message}");
        }
        
        return screenshots.Distinct().OrderBy(f => File.GetCreationTime(f)).ToList();
    }

    private string SanitizeDirectoryName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var result = name;
        
        foreach (var invalidChar in invalidChars)
        {
            result = result.Replace(invalidChar, '_');
        }
        
        return result.Replace(' ', '_').Replace(':', '_');
    }

    private string GenerateHtmlReport(List<TestResultSummary> testResults)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>UI Test Results - {DateTime.Now:yyyy-MM-dd HH:mm:ss}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; border-bottom: 2px solid #007acc; padding-bottom: 10px; }}
        h2 {{ color: #555; margin-top: 30px; }}
        .test-result {{ margin: 20px 0; padding: 15px; border-radius: 5px; }}
        .test-passed {{ background-color: #d4edda; border: 1px solid #c3e6cb; }}
        .test-failed {{ background-color: #f8d7da; border: 1px solid #f5c6cb; }}
        .test-skipped {{ background-color: #fff3cd; border: 1px solid #ffeaa7; }}
        .screenshot-gallery {{ display: flex; flex-wrap: wrap; gap: 10px; margin: 15px 0; }}
        .screenshot {{ border: 1px solid #ddd; border-radius: 4px; padding: 5px; background: white; }}
        .screenshot img {{ max-width: 200px; max-height: 150px; display: block; }}
        .screenshot-title {{ font-size: 12px; margin: 5px 0; color: #666; text-align: center; }}
        .summary {{ background: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .summary-stats {{ display: flex; justify-content: space-around; text-align: center; }}
        .stat {{ padding: 10px; }}
        .stat-value {{ font-size: 24px; font-weight: bold; }}
        .passed {{ color: #28a745; }}
        .failed {{ color: #dc3545; }}
        .skipped {{ color: #ffc107; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>UI Test Results</h1>
        <div class='summary'>
            <div class='summary-stats'>
                <div class='stat'>
                    <div class='stat-value passed'>{testResults.Count(r => r.Outcome == UnitTestOutcome.Passed)}</div>
                    <div>Passed</div>
                </div>
                <div class='stat'>
                    <div class='stat-value failed'>{testResults.Count(r => r.Outcome == UnitTestOutcome.Failed)}</div>
                    <div>Failed</div>
                </div>
                <div class='stat'>
                    <div class='stat-value skipped'>{testResults.Count(r => r.Outcome == UnitTestOutcome.Inconclusive)}</div>
                    <div>Skipped</div>
                </div>
                <div class='stat'>
                    <div class='stat-value'>{testResults.Sum(r => r.Duration.TotalSeconds):F1}s</div>
                    <div>Total Time</div>
                </div>
            </div>
        </div>";

        foreach (var testResult in testResults.OrderBy(r => r.TestName))
        {
            var cssClass = testResult.Outcome switch
            {
                UnitTestOutcome.Passed => "test-passed",
                UnitTestOutcome.Failed => "test-failed",
                _ => "test-skipped"
            };

            html += $@"
        <div class='test-result {cssClass}'>
            <h2>{testResult.TestName}</h2>
            <p><strong>Outcome:</strong> {testResult.Outcome}</p>
            <p><strong>Duration:</strong> {testResult.Duration.TotalSeconds:F2} seconds</p>";

            if (!string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                html += $"<p><strong>Error:</strong> {System.Web.HttpUtility.HtmlEncode(testResult.ErrorMessage)}</p>";
            }

            var screenshots = GetTestScreenshots(testResult.TestName);
            if (screenshots.Any())
            {
                html += "<h3>Screenshots</h3><div class='screenshot-gallery'>";
                foreach (var screenshot in screenshots)
                {
                    var relativePath = Path.GetRelativePath(_currentTestRunPath, screenshot);
                    var fileName = Path.GetFileName(screenshot);
                    html += $@"
                    <div class='screenshot'>
                        <img src='{relativePath}' alt='{fileName}' onclick='window.open(this.src)' style='cursor: pointer;' />
                        <div class='screenshot-title'>{fileName}</div>
                    </div>";
                }
                html += "</div>";
            }

            html += "</div>";
        }

        html += @"
    </div>
</body>
</html>";

        return html;
    }

    // Properties for external access
    public string CurrentTestRunPath => _currentTestRunPath;
    public string ScreenshotsPath => _screenshotsPath;
    public string ComparisonsPath => _comparisonsPath;
    public string BaselinePath => _baselinePath;
}

/// <summary>
/// Metadata about a test execution
/// </summary>
public class TestMetadata
{
    public string TestName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public UnitTestOutcome Outcome { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Screenshots { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Summary of test results for report generation
/// </summary>
public class TestResultSummary
{
    public string TestName { get; set; } = string.Empty;
    public UnitTestOutcome Outcome { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Screenshots { get; set; } = new();
}