using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace DailyTaskManager.Avalonia.Views;

/// <summary>
/// The first thing on screen: it appears as soon as the UI toolkit is up, while the database, the saved theme and the remembered
/// session are still being read. The login or main window replaces it. Built in code (no XAML to load) and with fixed colours, so
/// it needs nothing that is still loading.
/// </summary>
public sealed class SplashWindow : Window
{
    private readonly TextBlock _message;
    private readonly ProgressBar _progress;

    public SplashWindow()
    {
        Title = "Daily Task Manager";
        Width = 360;
        Height = 200;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#FFFFFFFF"));

        _message = new TextBlock
        {
            Text = "Starting…",
            Foreground = new SolidColorBrush(Color.Parse("#FF5B6770")),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };
        _progress = new ProgressBar { IsIndeterminate = true, Width = 160, Height = 4 };

        Content = new StackPanel
        {
            Spacing = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24),
            Children =
            {
                new TextBlock
                {
                    Text = "Daily Task Manager",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#FF111827")),
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                _progress,
                _message
            }
        };
    }

    /// <summary>The start-up failed: the reason stays on screen instead of the app disappearing</summary>
    public void ShowFailure(string message)
    {
        _progress.IsVisible = false;
        _message.Text = message;
        _message.Foreground = new SolidColorBrush(Color.Parse("#FFB42318"));
        Height = 260;
        Width = 480;
    }
}
