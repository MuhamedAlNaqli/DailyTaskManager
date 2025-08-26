using DailyTaskManager.App.ViewModels;
using System;
using System.Windows;

namespace DailyTaskManager.App
{
    public partial class AddTaskWindow : Window
    {
        private readonly AddTaskViewModel _viewModel;

        public AddTaskWindow()
        {
            InitializeComponent();
            _viewModel = (AddTaskViewModel)DataContext;
            
            // Subscribe to view model events
            _viewModel.WindowCloseRequested += OnWindowCloseRequested;
            _viewModel.TaskSaved += OnTaskSaved;

            // Set focus to description field when window loads
            Loaded += (s, e) => MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
        }

        private void OnWindowCloseRequested(object sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnTaskSaved(object sender, TaskSavedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            if (_viewModel != null)
            {
                _viewModel.WindowCloseRequested -= OnWindowCloseRequested;
                _viewModel.TaskSaved -= OnTaskSaved;
            }
            
            base.OnClosed(e);
        }
    }
}