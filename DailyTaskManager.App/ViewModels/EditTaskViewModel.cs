using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Services;
using DailyTaskManager.App.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DailyTaskManager.App.ViewModels
{
    public class EditTaskViewModel : INotifyPropertyChanged
    {
        private int _id;
        private string _description;
        private int _hours;
        private DateTime _date;
        private bool _isSubmitting;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public int Hours
        {
            get => _hours;
            set { _hours = value; OnPropertyChanged(); }
        }

        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        public bool IsSubmitting
        {
            get => _isSubmitting;
            set { _isSubmitting = value; OnPropertyChanged(); }
        }

        public ICommand SubmitCommand { get; }

        public event EventHandler<bool> RequestClose;

        public EditTaskViewModel(DailyTask task)
        {
            Id = task.Id;
            Description = task.Description;
            Hours = task.Hours;
            Date = task.Date;

            SubmitCommand = new RelayCommand(async _ => await SubmitAsync(), _ => !IsSubmitting);
        }

        private async Task SubmitAsync()
        {
            IsSubmitting = true;
            var updatedTask = new DailyTask
            {
                Id = Id,
                Description = Description,
                Hours = Hours,
                Date = Date
            };
            await DatabaseService.UpdateTaskAsync(updatedTask);
            IsSubmitting = false;
            RequestClose?.Invoke(this, true);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}