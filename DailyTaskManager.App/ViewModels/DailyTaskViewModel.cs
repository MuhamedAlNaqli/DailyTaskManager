#pragma warning disable CS4014
using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;

namespace DailyTaskManager.App.ViewModels
{
    public class DailyTaskViewModel : INotifyPropertyChanged
    {
        private readonly IExportService _exportService;
        private DailyTask _selectedTask;
        private string _searchText = String.Empty;
        private DateTime? _dateFrom;
        private DateTime? _dateTo;
        private readonly ICollectionView _filteredTasksView;
        private bool _isLoading;

        public ObservableCollection<DailyTask> Tasks { get; } = new ObservableCollection<DailyTask>();

        public ICollectionView FilteredTasks => _filteredTasksView;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    _filteredTasksView.Refresh();
                }
            }
        }

        public DateTime? DateFrom
        {
            get => _dateFrom;
            set
            {
                if (_dateFrom != value)
                {
                    _dateFrom = value;
                    OnPropertyChanged();
                    _filteredTasksView.Refresh();
                }
            }
        }

        public DateTime? DateTo
        {
            get => _dateTo;
            set
            {
                if (_dateTo != value)
                {
                    _dateTo = value;
                    OnPropertyChanged();
                    _filteredTasksView.Refresh();
                }
            }
        }


        public DailyTask SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand AddNewTaskCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand LoadTasksCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand EditCommand { get; }


        public DailyTaskViewModel()
        {
            _exportService = new ExportService();
            
            AddNewTaskCommand = new RelayCommand(_ => OpenAddTaskWindow());
            DeleteCommand = new RelayCommand(DeleteTask, _ => SelectedTask != null);
            LoadTasksCommand = new RelayCommand(async _ => await LoadTasksAsync());
            ExportToExcelCommand = new RelayCommand(async _ => await ExportToExcelAsync());
            ExportToPdfCommand = new RelayCommand(async _ => await ExportToPdfAsync());
            EditCommand = new RelayCommand(EditTask);


            _filteredTasksView = CollectionViewSource.GetDefaultView(Tasks);
            _filteredTasksView.Filter = FilterTasks;

            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await LoadTasksAsync();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private bool FilterTasks(object obj)
        {
            if (obj is DailyTask task)
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                    || task.Description.ToLower().Contains(SearchText.ToLower())
                    || task.Date.ToString("d").Contains(SearchText)
                    || task.Hours.ToString().Contains(SearchText);

                bool matchesDateFrom = !DateFrom.HasValue || task.Date >= DateFrom.Value;
                bool matchesDateTo = !DateTo.HasValue || task.Date <= DateTo.Value;

                return matchesSearch && matchesDateFrom && matchesDateTo;
            }
            return false;
        }

        private void OpenAddTaskWindow()
        {
            var addTaskWindow = new AddTaskWindow();
            addTaskWindow.Owner = Application.Current.MainWindow;
            
            if (addTaskWindow.ShowDialog() == true)
            {
                _ = LoadTasksAsync();
            }
        }

        private async void DeleteTask(object obj)
        {
            try
            {
                IsLoading = true;

                if (SelectedTask == null)
                {
                    return;
                }

                await DatabaseService.RemoveTaskAsync(SelectedTask.Id);
                await LoadTasksAsync();

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _filteredTasksView.Refresh();
                }), System.Windows.Threading.DispatcherPriority.Background);

                IsLoading = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadTasksAsync()
        {
            try
            {
                IsLoading = true;
                Tasks.Clear();

                var tasks = await DatabaseService.GetTasksAsync();

                foreach (var task in tasks)
                    Tasks.Add(task);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _filteredTasksView.Refresh();
                }), System.Windows.Threading.DispatcherPriority.Background);

                IsLoading = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load daily tasks: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportToExcelAsync()
        {
            try
            {
                IsLoading = true;
                await _exportService.ExportToExcelAsync(Tasks);
                
                MessageBox.Show("Excel export completed successfully!", "Export Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportToPdfAsync()
        {
            try
            {
                IsLoading = true;
                await _exportService.ExportToPdfAsync(Tasks);
                
                MessageBox.Show("PDF export completed successfully!", "Export Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to PDF: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void EditTask(object obj)
        {
            if (obj is DailyTask taskToEdit)
            {
                var editWindow = new EditTaskWindow(taskToEdit);
                if (editWindow.ShowDialog() == true)
                {
                    _ = LoadTasksAsync();
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}