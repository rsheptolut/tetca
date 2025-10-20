using Tetca.ActivityDetectors;
using Tetca.Logic;
using Tetca.Notifiers;
using Tetca.Notifiers.Speech;
using Tetca.Windows.MainWindow;
using Tetca.Windows.NotifyIconMenu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Tetca.Helpers;
using System.Windows;

namespace Tetca
{
    /// <summary>
    /// Represents the startup logic for the Tetca application.
    /// Handles application initialization, configuration, and dependency injection setup.
    /// </summary>
    internal class Startup
    {
        /// <summary>
        /// Gets the service provider used for dependency injection.
        /// </summary>
        public ServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets or sets the main application instance.
        /// </summary>
        public App App { get; set; }

        /// <summary>
        /// Initializes the application by setting up configuration and services.
        /// </summary>
        /// <param name="app">The main application instance.</param>
        public void InitializeApplication(App app)
        {
            this.App = app;

            app.DispatcherUnhandledException += this.App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;

            // App configuration
            var configuration = this.SetupAppConfiguration();

            // Services
            var services = new ServiceCollection();
            this.ConfigureServices(services);

            // Bind settings
            var settings = new Settings();
            configuration.Bind(settings);
            services.AddSingleton(settings);

            // Build service provider
            this.ServiceProvider = services.BuildServiceProvider();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var logger = this.ServiceProvider.GetService<ILogger<App>>();
            if (logger != null && e.ExceptionObject is Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in AppDomain.");
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var logger = this.ServiceProvider.GetService<ILogger<App>>();
            if (logger != null && e.Exception is not null)
            {
                logger.LogError(e.Exception, "Unhandled exception.");
            }
        }

        /// <summary>
        /// Sets up the application configuration by loading settings from JSON files.
        /// Creates a default configuration file if none exists.
        /// </summary>
        /// <returns>The application configuration.</returns>
        private IConfiguration SetupAppConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();

            if (File.Exists("appsettings.debug.json"))
            {
                configurationBuilder.AddJsonFile($"appsettings.debug.json");
                configurationBuilder.AddUserSecrets<App>();
            }
            else
            {
                if (!File.Exists("../appsettings.json"))
                {
                    var defaultSettings = EmbeddedResources.ResourceStream("appsettings.default.json");
                    using var streamReader = new StreamReader(defaultSettings);
                    var defaultSettingsContent = streamReader.ReadToEnd();
                    try
                    {
                        File.WriteAllText("../appsettings.json", defaultSettingsContent);
                    }
                    catch
                    {
                        MessageBox.Show($"Unable to create appsettings.json file in directory {Path.GetFullPath("..")}. Please ensure the app has write access to this directory, as it will be storing settings, logs and reports there. It's recommended to put {App.Name}.exe into %USERPROFILE%\\Roaming\\${App.Name}\\app", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.App.Shutdown();
                        return null;
                    }

                    MessageBox.Show($"{App.Name} has started and is hiding in your system tray. Configuration is done via appsettings.json file in the parent folder. You will only see this notice once.", App.Name);
                }

                configurationBuilder.AddJsonFile(Path.GetFullPath("../appsettings.json"));
            }

            var configuration = configurationBuilder.Build();
            return configuration;
        }

        /// <summary>
        /// Configures the services required by the application.
        /// Adds dependencies to the service collection for dependency injection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private void ConfigureServices(ServiceCollection services)
        {
            // Add logging
            services.AddLogging(logging =>
            { 
                logging.AddDebug();

                logging.AddFiles(b =>
                {
                    b.AddFile("sf")
                        .WithOptions(o =>
                        {
                            o.Path = Path.GetFullPath("../Tetca.log");
                            o.Append = true;
                        })
                        .WithSimpleFormatter(f =>
                        {
                            f.IncludeScopes = true;
                            f.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
                            f.InvariantTimestampFormat = true;
                            f.SingleLine = true;
                            f.EmptyLineBetweenMessages = false;
                        });
                });
                
                logging.AddFilter("Tetca", LogLevel.Debug);
            });

            // Register application services
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainLoop>();
            services.AddSingleton<ISpeech, DefaultSpeech>();
            services.AddSingleton<InputDetector>();
            services.AddSingleton<CallDetector>();
            services.AddSingleton<DoNotDisturb>();
            services.AddSingleton<SoundDeviceManager>(sp =>
            {
                var settings = sp.GetRequiredService<Settings>();
                return new SoundDeviceManager(settings.VoiceNotificationSoundDevice);
            });
            services.AddSingleton<HumanEscalator>();
            services.AddSingleton(this.App.Dispatcher);
            services.AddSingleton<DebugState>();
            services.AddSingleton<NotifyIconLogic>();
            services.AddSingleton<ApplicationCore>();
            services.AddSingleton<ICurrentTime, CurrentTime>();

            // Register WorkRecorder with custom initialization
            services.AddSingleton<WorkRecorder>();

            // Register DeliberateActivityFilter with custom initialization
            services.AddSingleton<DeliberateActivityFilter>(sp =>
            {
                var settings = sp.GetRequiredService<Settings>();
                var currentTime = sp.GetRequiredService<ICurrentTime>();
                var deliberateActivityFilter = new DeliberateActivityFilter(settings.MinCheckCadence, TimeSpan.FromMinutes(1.5), currentTime);
                return deliberateActivityFilter;
            });
        }
    }
}
