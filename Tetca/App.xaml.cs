using Tetca.Helpers;
using Tetca.Logic;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Tetca
{
    /// <summary>
    /// Represents the main application class for the Tetca WPF application.
    /// Handles application startup, shutdown, and core logic initialization.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The chat bubble window used for displaying break reminders.
        /// </summary>
        private ChatBubble chatBubble;

        /// <summary>
        /// The service provider for dependency injection.
        /// </summary>
        private ServiceProvider serviceProvider;

        /// <summary>
        /// The core application logic, responsible for managing the main loop and notifications.
        /// </summary>
        private ApplicationCore core;

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        public static string Name => "Tetca";

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
        }

        /// <summary>
        /// Handles the application startup event.
        /// Initializes the application core and starts the main loop.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The startup event arguments.</param>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show($"Another instance of {App.Name} is already running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }

            var startup = new Startup();
            startup.InitializeApplication(this);
            this.serviceProvider = startup.ServiceProvider;

            this.core = this.serviceProvider.GetRequiredService<ApplicationCore>(); // Root service that is required for startup

            this.core.MainLoop.ActivityCheckPerformed += new EventHandler(this.ActivityCheckPerformed);
            this.core.MainLoop.BreakSuggested += new EventHandler(this.BreakSuggested);
            this.core.NotifyIconLogic.DoubleClick += this.NotifyIcon_DoubleClicked;
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(this.PowerModeChanged);

            this.core.MainLoop.Start();
        }

        /// <summary>
        /// Handles the event when an activity check is performed.
        /// Updates the main window title and refreshes the view model.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ActivityCheckPerformed(object sender, EventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.core.MainWindow.Title = this.core.MainLoop.ActivityTime.ToHoursAndMinutes() + " | " + App.Name;
                    this.core.MainWindow.ViewModel.RefreshAll();
                });
            }
            catch (TaskCanceledException)
            {
                // That's fine!
            }
        }

        /// <summary>
        /// Handles the event when the notify icon is double-clicked.
        /// Restores and shows the main window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void NotifyIcon_DoubleClicked(object sender, EventArgs e)
        {
            if (this.core.MainWindow.WindowState != WindowState.Normal || !this.core.MainWindow.IsVisible)
            {
                this.core.MainWindow.WindowState = WindowState.Normal;
                this.core.MainWindow.Show();
            }
            else
            {
                this.core.MainWindow.Hide();
            }
        }

        /// <summary>
        /// Handles the event when a break is suggested.
        /// Displays a chat bubble window to notify the user.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BreakSuggested(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.chatBubble?.Close();
                this.chatBubble = new ChatBubble();
                this.chatBubble.Top = new Random().Next(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Bottom - (int)this.chatBubble.Height);
                this.chatBubble.Left = new Random().Next(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right - (int)this.chatBubble.Width);
                this.chatBubble.ShowInTaskbar = false;
                this.chatBubble.Topmost = true;
                this.chatBubble.Show();
            });
        }

        /// <summary>
        /// Handles the event when the system power mode changes.
        /// Rechecks activity if the system resumes from a suspended state.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The power mode change event arguments.</param>
        private void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                this.core.MainLoop.Recheck();
            }
        }

        /// <summary>
        /// Handles the application exit event.
        /// Disposes of the main loop and notify icon logic.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The exit event arguments.</param>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            this.core.MainLoop.Dispose();
            this.core.NotifyIconLogic.Dispose();
        }
    }
}
