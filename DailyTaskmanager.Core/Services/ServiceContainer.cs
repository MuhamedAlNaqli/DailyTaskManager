using System;
using System.Collections.Generic;

namespace DailyTaskManager.Core.Services
{
    /// <summary>
    /// Simple dependency injection container for .NET Framework
    /// </summary>
    public class ServiceContainer
    {
        private static readonly Lazy<ServiceContainer> _instance = new Lazy<ServiceContainer>(() => new ServiceContainer());
        private readonly Dictionary<Type, Func<object>> _services;
        private readonly Dictionary<Type, object> _singletons;

        public static ServiceContainer Instance => _instance.Value;

        private ServiceContainer()
        {
            _services = new Dictionary<Type, Func<object>>();
            _singletons = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Register a transient service (new instance each time)
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _services[typeof(TInterface)] = () => Activator.CreateInstance<TImplementation>();
        }

        /// <summary>
        /// Register a transient service with a factory method
        /// </summary>
        public void RegisterTransient<TInterface>(Func<TInterface> factory)
        {
            _services[typeof(TInterface)] = () => factory();
        }

        /// <summary>
        /// Register a singleton service (same instance each time)
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _services[typeof(TInterface)] = () =>
            {
                if (!_singletons.ContainsKey(typeof(TInterface)))
                {
                    _singletons[typeof(TInterface)] = Activator.CreateInstance<TImplementation>();
                }
                return _singletons[typeof(TInterface)];
            };
        }

        /// <summary>
        /// Register a singleton service with a factory method
        /// </summary>
        public void RegisterSingleton<TInterface>(Func<TInterface> factory)
        {
            _services[typeof(TInterface)] = () =>
            {
                if (!_singletons.ContainsKey(typeof(TInterface)))
                {
                    _singletons[typeof(TInterface)] = factory();
                }
                return _singletons[typeof(TInterface)];
            };
        }

        /// <summary>
        /// Register a singleton instance
        /// </summary>
        public void RegisterInstance<TInterface>(TInterface instance)
        {
            _singletons[typeof(TInterface)] = instance;
            _services[typeof(TInterface)] = () => instance;
        }

        /// <summary>
        /// Resolve a service
        /// </summary>
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// Resolve a service by type
        /// </summary>
        public object Resolve(Type type)
        {
            if (_services.ContainsKey(type))
            {
                return _services[type]();
            }

            throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Configure all application services
        /// </summary>
        public static void ConfigureServices()
        {
            var container = Instance;
            
            // Register speech recognition service with fallback mechanism
            container.RegisterSingleton<ISpeechRecognitionService>(() => CreateSpeechRecognitionService());
            
            // Register export service as transient
            container.RegisterTransient<IExportService, ExportService>();
        }

        /// <summary>
        /// Creates speech recognition service with fallback from Whisper to System.Speech
        /// </summary>
        private static ISpeechRecognitionService CreateSpeechRecognitionService()
        {
            // Since Whisper integration is not complete, directly use System.Speech
            // This avoids any blocking initialization that could interfere with app startup
            System.Diagnostics.Debug.WriteLine("ServiceContainer: Using SpeechRecognitionService (System.Speech) - Whisper integration pending");
            return new SpeechRecognitionService();
        }

        /// <summary>
        /// Clear all registrations (useful for testing)
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            
            // Dispose singletons that implement IDisposable
            foreach (var singleton in _singletons.Values)
            {
                if (singleton is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _singletons.Clear();
        }
    }
}