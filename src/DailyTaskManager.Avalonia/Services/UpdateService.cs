using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NetSparkleUpdater;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.Avalonia;
using NetSparkleUpdater.Events;
using NetSparkleUpdater.Enums;
using Avalonia.Controls.ApplicationLifetimes;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>
/// Service for handling application updates using NetSparkleUpdater
/// </summary>
public class UpdateService : IUpdateService, IDisposable
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IConfiguration _configuration;
    private SparkleUpdater? _sparkleUpdater;
    private Timer? _updateTimer;
    private bool _disposed = false;

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<UpdateCheckCompletedEventArgs>? UpdateCheckCompleted;

    public bool AutoUpdatesEnabled { get; set; } = true;

    public UpdateService(ILogger<UpdateService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing UpdateService...");

            // Get update configuration
            // Using latest release download URL works for both public and private repos
            var appCastUrl = _configuration["UpdateService:AppCastUrl"]
                ?? "https://github.com/MuhamedAlNaqli/DailyTaskManager/releases/latest/download/appcast.xml";
            
            var applicationName = _configuration["UpdateService:ApplicationName"] ?? "DailyTaskManager";

            _logger.LogInformation("AppCast URL: {AppCastUrl}", appCastUrl);

            // Initialize SparkleUpdater with minimal configuration for now
            // Verify update signatures whenever a public key is configured
            var publicKey = _configuration["UpdateService:Ed25519PublicKey"];
            Ed25519Checker checker;
            if (!string.IsNullOrWhiteSpace(publicKey))
            {
                checker = new Ed25519Checker(NetSparkleUpdater.Enums.SecurityMode.Strict, publicKey);
            }
            else
            {
                _logger.LogWarning("UpdateService:Ed25519PublicKey is not configured - update signatures are NOT verified");
                checker = new Ed25519Checker(NetSparkleUpdater.Enums.SecurityMode.Unsafe);
            }

            _sparkleUpdater = new SparkleUpdater(appCastUrl, checker);

            // Configure UI factory
            _sparkleUpdater.UIFactory = new UIFactory();

            // Subscribe to events  
            _sparkleUpdater.UpdateDetected += OnUpdateDetected;
            _sparkleUpdater.UpdateCheckFinished += OnUpdateCheckFinished;

            // Start update service
            _sparkleUpdater.StartLoop(true, true);

            _logger.LogInformation("UpdateService initialized successfully");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UpdateService");
            throw;
        }
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for updates manually...");
            
            if (_sparkleUpdater == null)
            {
                _logger.LogWarning("SparkleUpdater not initialized");
                return false;
            }

            var updateInfo = await _sparkleUpdater.CheckForUpdatesQuietly();
            return updateInfo != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            UpdateCheckCompleted?.Invoke(this, new UpdateCheckCompletedEventArgs
            {
                UpdateAvailable = false,
                CheckSuccessful = false,
                ErrorMessage = ex.Message
            });
            return false;
        }
    }

    public void StartAutomaticUpdateChecking(TimeSpan interval)
    {
        try
        {
            if (!AutoUpdatesEnabled)
            {
                _logger.LogInformation("Automatic updates disabled");
                return;
            }

            _logger.LogInformation("Starting automatic update checking with interval: {Interval}", interval);

            _updateTimer?.Stop();
            _updateTimer?.Dispose();

            _updateTimer = new Timer(interval.TotalMilliseconds);
            _updateTimer.Elapsed += async (sender, e) =>
            {
                try
                {
                    await CheckForUpdatesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic update check");
                }
            };
            _updateTimer.AutoReset = true;
            _updateTimer.Start();

            _logger.LogInformation("Automatic update checking started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start automatic update checking");
        }
    }

    public void StopAutomaticUpdateChecking()
    {
        try
        {
            _logger.LogInformation("Stopping automatic update checking");
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _updateTimer = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping automatic update checking");
        }
    }

    public async Task ShowUpdateUIAsync()
    {
        try
        {
            if (_sparkleUpdater == null)
            {
                _logger.LogWarning("SparkleUpdater not initialized");
                return;
            }

            await _sparkleUpdater.CheckForUpdatesAtUserRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing update UI");
        }
    }

    public string GetCurrentVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "2.1.0.0";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current version");
            return "2.1.0.0";
        }
    }

    private void OnUpdateDetected(object? sender, UpdateDetectedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Update detected: {Version}", e.LatestVersion?.Version?.ToString());

            if (e.LatestVersion != null)
            {
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                {
                    NewVersion = e.LatestVersion.Version?.ToString() ?? "Unknown",
                    ReleaseNotes = e.LatestVersion.Description ?? string.Empty,
                    ReleaseDate = e.LatestVersion.PublicationDate != default(DateTime) ? e.LatestVersion.PublicationDate : DateTime.Now,
                    IsCriticalUpdate = e.LatestVersion.IsCriticalUpdate
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update detected event");
        }
    }

    private void OnUpdateCheckFinished(object? sender, UpdateStatus status)
    {
        try
        {
            var updateRequired = status == UpdateStatus.UpdateAvailable;
            _logger.LogInformation("Update check finished. Status: {Status}, Update required: {UpdateRequired}", status, updateRequired);

            UpdateCheckCompleted?.Invoke(this, new UpdateCheckCompletedEventArgs
            {
                UpdateAvailable = updateRequired,
                CheckSuccessful = status != UpdateStatus.CouldNotDetermine,
                ErrorMessage = status == UpdateStatus.CouldNotDetermine ? "Could not determine update status" : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update check finished event");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _logger.LogInformation("Disposing UpdateService");

                StopAutomaticUpdateChecking();

                if (_sparkleUpdater != null)
                {
                    _sparkleUpdater.UpdateDetected -= OnUpdateDetected;
                    _sparkleUpdater.UpdateCheckFinished -= OnUpdateCheckFinished;
                    
                    _sparkleUpdater.Dispose();
                    _sparkleUpdater = null;
                }

                _disposed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing UpdateService");
            }
        }
    }
}