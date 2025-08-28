using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DailyTaskManager.App.ViewModels
{
    public class AddTaskViewModel : INotifyPropertyChanged
    {
        private string _description = string.Empty;
        private DateTime _selectedDate = DateTime.Today;
        private decimal _hours = 7;
        private string _validationError = string.Empty;
        private bool _hasValidationError = false;
        private bool _isSaving = false;

        public event EventHandler<TaskSavedEventArgs> TaskSaved;
        public event EventHandler WindowCloseRequested;

        public string Description
        {
            get { return _description; }
            set 
            { 
                _description = value; 
                OnPropertyChanged();
                ClearValidationError();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set 
            { 
                _selectedDate = value; 
                OnPropertyChanged();
                ClearValidationError();
            }
        }

        public decimal Hours
        {
            get { return _hours; }
            set 
            { 
                _hours = value; 
                OnPropertyChanged();
                ClearValidationError();
            }
        }

        public string ValidationError
        {
            get { return _validationError; }
            set 
            { 
                _validationError = value; 
                OnPropertyChanged();
                HasValidationError = !string.IsNullOrWhiteSpace(value);
            }
        }

        public bool HasValidationError
        {
            get { return _hasValidationError; }
            set 
            { 
                _hasValidationError = value; 
                OnPropertyChanged();
            }
        }

        public bool IsSaving
        {
            get { return _isSaving; }
            set 
            { 
                _isSaving = value; 
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddTaskViewModel()
        {
            SaveCommand = new RelayCommand(async _ => await SaveTaskAsync(), CanSave);
            CancelCommand = new RelayCommand(_ => CloseWindow());
        }

        private bool CanSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Description) && 
                   Hours > 0 && 
                   !IsSaving;
        }

        private async Task SaveTaskAsync()
        {
            try
            {
                if (!ValidateInput())
                    return;

                IsSaving = true;

                var task = new DailyTask
                {
                    Description = Description.Trim(),
                    Date = SelectedDate,
                    Hours = Hours
                };

                await DatabaseService.AddTaskAsync(task);

                if (TaskSaved != null)
                {
                    TaskSaved(this, new TaskSavedEventArgs(task));
                }

                CloseWindow();
            }
            catch (Exception ex)
            {
                ValidationError = "Error saving task: " + ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Description))
            {
                ValidationError = "Task description is required.";
                return false;
            }

            if (Description.Trim().Length < 3)
            {
                ValidationError = "Task description must be at least 3 characters long.";
                return false;
            }

            if (Hours <= 0)
            {
                ValidationError = "Hours must be greater than zero.";
                return false;
            }

            if (Hours > 24)
            {
                ValidationError = "Hours cannot exceed 24 per day.";
                return false;
            }

            return true;
        }

        private void ClearValidationError()
        {
            if (HasValidationError)
            {
                ValidationError = string.Empty;
            }
        }

        private void CloseWindow()
        {
            if (WindowCloseRequested != null)
            {
                WindowCloseRequested(this, EventArgs.Empty);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class TaskSavedEventArgs : EventArgs
    {
        public DailyTask Task { get; }

        public TaskSavedEventArgs(DailyTask task)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
        }
    }
}