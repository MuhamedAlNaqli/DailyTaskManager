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

namespace DailyTaskManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _description = String.Empty;
        private DateTime _selectedDate = DateTime.Now;
        private int _hours = 7;
        private DailyTask _selectedTask;
        private string _searchText = String.Empty;
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

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set { _selectedDate = value; OnPropertyChanged(); }
        }

        public int Hours
        {
            get => _hours;
            set { _hours = value; OnPropertyChanged(); }
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

        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand LoadTasksCommand { get; }
        public ICommand ExportTasksCommand { get; }

        public MainViewModel()
        {
            SaveCommand = new RelayCommand(async _ => await SaveTask());
            ClearCommand = new RelayCommand(_ => ClearFields());
            DeleteCommand = new RelayCommand(DeleteTask, _ => SelectedTask != null);
            LoadTasksCommand = new RelayCommand(async _ => await LoadTasksAsync());
            ExportTasksCommand = new RelayCommand(async _ => await ExportTasksAsync());

            // Setup filtered view
            _filteredTasksView = CollectionViewSource.GetDefaultView(Tasks);
            _filteredTasksView.Filter = FilterTasks;

            _ = LoadTasksAsync(); // Auto load on startup
        }

        private bool FilterTasks(object obj)
        {
            if (obj is DailyTask task)
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    return true;

                return task.Description.ToLower().Contains(SearchText.ToLower())
                       || task.Date.ToString("d").Contains(SearchText)
                       || task.Hours.ToString().Contains(SearchText);
            }

            return false;
        }

        private async Task SaveTask()
        {
            try
            {
                IsLoading = true;

                if (string.IsNullOrWhiteSpace(Description)) return;

                var task = new DailyTask
                {
                    Description = Description,
                    Date = SelectedDate,
                    Hours = Hours
                };

                await DatabaseService.AddTaskAsync(task);
                await LoadTasksAsync();
                ClearFields();

                IsLoading = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearFields()
        {
            Description = string.Empty;
            Hours = 7;
            SelectedDate = DateTime.Now;
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
                _filteredTasksView.Refresh();

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
