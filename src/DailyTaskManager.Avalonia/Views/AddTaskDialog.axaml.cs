using Avalonia.Controls;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.Avalonia.Views;

public partial class AddTaskDialog : Window
{
    public AddTaskDialog()
    {
        InitializeComponent();
        
        // Subscribe to ViewModel events
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is AddTaskViewModel viewModel)
        {
            viewModel.RequestClose += OnRequestClose;
        }
    }

    private void OnRequestClose(object? sender, bool dialogResult)
    {
        Close(dialogResult);
    }
}