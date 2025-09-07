using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>
/// Service for managing navigation between views
/// </summary>
public class NavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Creates a view with its associated ViewModel
    /// </summary>
    public TView CreateView<TView, TViewModel>()
        where TView : UserControl, new()
        where TViewModel : class
    {
        try
        {
            var view = new TView();
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            view.DataContext = viewModel;

            _logger.LogDebug("Created view {ViewType} with ViewModel {ViewModelType}",
                typeof(TView).Name, typeof(TViewModel).Name);

            return view;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating view {ViewType} with ViewModel {ViewModelType}",
                typeof(TView).Name, typeof(TViewModel).Name);
            throw;
        }
    }

    /// <summary>
    /// Creates a view with its associated ViewModel asynchronously on the UI thread
    /// </summary>
    public async Task<TView> CreateViewAsync<TView, TViewModel>()
        where TView : UserControl, new()
        where TViewModel : class
    {
        // Ensure UI controls are created on the UI thread
        return await Dispatcher.UIThread.InvokeAsync(() => CreateView<TView, TViewModel>());
    }

    /// <summary>
    /// Creates a view with a specific ViewModel instance
    /// </summary>
    public TView CreateViewWithViewModel<TView>(object viewModel)
        where TView : UserControl, new()
    {
        try
        {
            var view = new TView();
            view.DataContext = viewModel;

            _logger.LogDebug("Created view {ViewType} with provided ViewModel", typeof(TView).Name);

            return view;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating view {ViewType} with provided ViewModel", typeof(TView).Name);
            throw;
        }
    }
}