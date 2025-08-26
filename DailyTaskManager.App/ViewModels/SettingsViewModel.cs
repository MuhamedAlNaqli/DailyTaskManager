using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Services;
using DailyTaskManager.App.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.IO;
using System;
using System.Collections.Generic;

namespace DailyTaskManager.App.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _service = new SettingsService();
        private readonly IExportService _exportService = new ExportService();

        public ObservableCollection<Setting> DbSettings { get; } = new ObservableCollection<Setting>();
        public ObservableCollection<AppSettingItem> AppSettings { get; } = new ObservableCollection<AppSettingItem>();
        public PdfSettings PdfSettings { get; } = new PdfSettings();

        public SettingsViewModel()
        {
            LoadPdfSettings();
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
        public ICommand SavePdfSettingsCommand => new RelayCommand(async _ => await SavePdfSettingsAsync());
        public ICommand PreviewPdfCommand => new RelayCommand(async _ => await PreviewPdfAsync());

        private void AddSetting()
        {
            DbSettings.Add(new Setting { Key = "NewKey", Value = "" });
        }

        private async Task SaveAsync()
        {
            await _service.UpsertAsync(DbSettings.ToList());
        }

        private void LoadPdfSettings()
        {
            // Load PDF settings from database or use defaults
            try
            {
                // For now, we'll use the defaults set in PdfSettings constructor
                // In the future, this could load from database settings
            }
            catch (Exception)
            {
                // Use default settings if loading fails
            }
        }

        private async Task SavePdfSettingsAsync()
        {
            try
            {
                // Save PDF settings to database
                var pdfSettingsList = new List<Setting>
                {
                    new Setting { Key = "PDF_CompanyName", Value = PdfSettings.CompanyName ?? "" },
                    new Setting { Key = "PDF_CompanyId", Value = PdfSettings.CompanyId ?? "" },
                    new Setting { Key = "PDF_AddressLine1", Value = PdfSettings.AddressLine1 ?? "" },
                    new Setting { Key = "PDF_AddressLine2", Value = PdfSettings.AddressLine2 ?? "" },
                    new Setting { Key = "PDF_Phone", Value = PdfSettings.Phone ?? "" },
                    new Setting { Key = "PDF_Email", Value = PdfSettings.Email ?? "" },
                    new Setting { Key = "PDF_ClientName", Value = PdfSettings.ClientName ?? "" },
                    new Setting { Key = "PDF_ClientId", Value = PdfSettings.ClientId ?? "" },
                    new Setting { Key = "PDF_DocumentTitle", Value = PdfSettings.DocumentTitle ?? "" },
                    new Setting { Key = "PDF_ContractPrefix", Value = PdfSettings.ContractPrefix ?? "" },
                    new Setting { Key = "PDF_ShowLogoBorder", Value = PdfSettings.ShowLogoBorder.ToString() },
                    new Setting { Key = "PDF_IncludeSignatures", Value = PdfSettings.IncludeSignatures.ToString() }
                };

                await _service.UpsertAsync(pdfSettingsList);
                MessageBox.Show("PDF settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving PDF settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PreviewPdfAsync()
        {
            try
            {
                // Create sample tasks for preview
                var sampleTasks = new List<DailyTask>
                {
                    new DailyTask { Description = "Sample Task 1 - Development work", Date = DateTime.Now.AddDays(-2), Hours = 8 },
                    new DailyTask { Description = "Sample Task 2 - Testing and debugging", Date = DateTime.Now.AddDays(-1), Hours = 6 },
                    new DailyTask { Description = "Sample Task 3 - Documentation update", Date = DateTime.Now, Hours = 4 }
                };

                // Generate preview with current settings
                await _exportService.ExportToPdfAsync(sampleTasks);
                MessageBox.Show("Preview PDF generated successfully! Check your desktop for the HTML file.", "Preview Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating PDF preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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