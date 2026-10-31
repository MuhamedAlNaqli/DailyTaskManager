using Avalonia.Controls;

namespace DailyTaskManager.Avalonia.EmployeeMode;

public partial class EmployeeWindow : Window
{
    public EmployeeWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            if (DataContext is EmployeeViewModel viewModel) await viewModel.LoadAsync();
        };
    }
}
