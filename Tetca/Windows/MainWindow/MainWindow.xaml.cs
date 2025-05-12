using System;
using System.ComponentModel;
using System.Windows;
using Tetca.Windows.MainWindow;

namespace Tetca
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            this.ViewModel = viewModel;
            this.WindowState = WindowState.Minimized;
            this.Hide();
            this.InitializeComponent();
            this.DataContext = viewModel;

            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// The view model for the main window.
        /// </summary>
        public MainWindowViewModel ViewModel { get; }

        // Minimize to system tray when application is minimized.
        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized) this.Hide();

            base.OnStateChanged(e);
        }

        /// <summary>
        /// Minimize to system tray when application is closed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            // setting cancel to true will cancel the close request
            // so the application is not closed
            e.Cancel = true;

            this.Hide();

            base.OnClosing(e);
        }

        /// <summary>
        /// Show the application when the notify icon is double clicked.
        /// </summary>
        private void NotifyIcon_DoubleClicked(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Show();
        }
    }
}
