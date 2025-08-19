using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Services;
using DailyTaskManager.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DailyTaskManager.App.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _service = new SettingsService();

        public ObservableCollection<Setting> DbSettings { get; } = new ObservableCollection<Setting>();
        public ObservableCollection<AppSettingItem> AppSettings { get; } = new ObservableCollection<AppSettingItem>();

        public SettingsViewModel()
        {
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            // Load DB settings
            DbSettings.Clear();
            try
            {
                var all = await _service.GetAllAsync();
                foreach (var s in all) DbSettings.Add(s);
            }
            catch
            {
                // ignore in case DB is unavailable
            }

            // Load App.config (read-only)
            AppSettings.Clear();
            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                AppSettings.Add(new AppSettingItem { Key = key, Value = ConfigurationManager.AppSettings[key] });
            }

            // Also add connection string name + redacted
            var cs = ConfigurationManager.ConnectionStrings["PostgresConnection"]?.ConnectionString ?? "(missing)";
            AppSettings.Add(new AppSettingItem { Key = "ConnectionStrings:PostgresConnection", Value = cs });
        }

        public ICommand AddSettingCommand => new RelayCommand(_ => AddSetting());
        public ICommand SaveCommand => new RelayCommand(async _ => await SaveAsync(), _ => DbSettings.Any());

        private void AddSetting()
        {
            DbSettings.Add(new Setting { Key = "NewKey", Value = "" });
        }

        private async Task SaveAsync()
        {
            await _service.UpsertAsync(DbSettings.ToList());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AppSettingItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}