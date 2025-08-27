using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DailyTaskManager.Core.Services;

namespace DailyTaskManager.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Configure dependency injection container
            ServiceContainer.ConfigureServices();
            
            base.OnStartup(e);
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up services
            ServiceContainer.Instance.Clear();
            
            base.OnExit(e);
        }
    }
}
