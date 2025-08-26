using DailyTaskManager.Core.Models;
using DailyTaskManager.Export;
using DailyTaskManager.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using DailyTaskManager.App;
using DailyTaskManager.App.ViewModels;
using System.Windows;

namespace DailyTaskManager.App.ViewModels
{
    public class DailyTaskViewModel : INotifyPropertyChanged
    {
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
                    _filteredTasksView.Refresh(); // trigger filter
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
        public ICommand ExportTasksCommand { get; }
        public ICommand EditCommand { get; }


        public DailyTaskViewModel()
        {
            AddNewTaskCommand = new RelayCommand(_ => OpenAddTaskWindow());
            DeleteCommand = new RelayCommand(DeleteTask, _ => SelectedTask != null);
            LoadTasksCommand = new RelayCommand(async _ => await LoadTasksAsync());
            ExportTasksCommand = new RelayCommand(async _ => await ExportTasksAsync());
            EditCommand = new RelayCommand(EditTask);


            // Setup filtered view
            _filteredTasksView = CollectionViewSource.GetDefaultView(Tasks);
            _filteredTasksView.Filter = FilterTasks;

            _ = LoadTasksAsync(); // Auto load on startup
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
                // Task was saved successfully, refresh the list
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

                // Force UI refresh to prevent rendering issues
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

                // Force UI refresh after loading
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

        private async Task ExportTasksAsync()
        {
            try
            {
                IsLoading = true;

                await Task.Run(() =>
                {
                    ExcelExporter exporter = new ExcelExporter();
                    exporter.ExportToExcel(Tasks);
                });

                IsLoading = false;
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
                    // After editing, reload tasks to reflect changes
                    _ = LoadTasksAsync();
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}