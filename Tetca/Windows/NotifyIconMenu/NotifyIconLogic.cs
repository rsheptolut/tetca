using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Tetca.Helpers;
using Tetca.Logic;
using Tetca.Notifiers.Speech;

namespace Tetca.Windows.NotifyIconMenu
{
    /// <summary>
    /// Manages the logic for the system tray icon, including its menu and interactions.
    /// </summary>
    public class NotifyIconLogic : IDisposable
    {
        private readonly NotifyIcon Icon;
        private readonly ISpeech speech;
        private readonly Dispatcher dispatcher;
        private readonly MainLoop mainLoop;
        private readonly WorkRecorder workRecorder;
        private ToolStripMenuItem TimeDisplay;

        /// <summary>
        /// Occurs when the tray icon is double-clicked.
        /// </summary>
        public event EventHandler DoubleClick;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyIconLogic"/> class.
        /// </summary>
        /// <param name="speech">The speech notification service.</param>
        /// <param name="dispatcher">The dispatcher for UI thread operations.</param>
        /// <param name="mainLoop">The main application loop for activity tracking.</param>
        public NotifyIconLogic(ISpeech speech, Dispatcher dispatcher, MainLoop mainLoop, WorkRecorder workRecorder)
        {
            this.speech = speech;
            this.dispatcher = dispatcher;
            this.mainLoop = mainLoop;
            this.workRecorder = workRecorder;
            mainLoop.ActivityCheckPerformed += new EventHandler(this.ActivityCheckPerformed);
            this.Icon = new NotifyIcon()
            {
                Visible = true,
                ContextMenuStrip = this.BuildContextMenu(),
                Icon = new System.Drawing.Icon(EmbeddedResources.ResourceStream("app.ico"))
            };
            this.Icon.DoubleClick += (sender, args) => DoubleClick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates the tray icon and menu with the latest activity time.
        /// </summary>
        private void ActivityCheckPerformed(object sender, EventArgs e)
        {
            try
            {
                this.dispatcher.Invoke(() =>
                {
                    this.TimeDisplay.Text = this.mainLoop.ActivityTime.ToHoursAndMinutesLong() + " since last break";
                    this.Icon.Text = this.mainLoop.ActivityTime.ToHoursAndMinutes() + " | " + App.Name;
                });
            }
            catch (TaskCanceledException)
            {
                // That's fine!
            }
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="NotifyIconLogic"/> instance.
        /// </summary>
        public void Dispose()
        {
            this.dispatcher.Invoke(() =>
            {
                this.Icon.Visible = false;
            });
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Builds the context menu for the tray icon.
        /// </summary>
        /// <returns>A <see cref="ContextMenuStrip"/> with menu items.</returns>
        private ContextMenuStrip BuildContextMenu()
        {
            var result = new ContextMenuStrip();
            result.Items.AddRange(this.BuildMenuItems().ToArray());
            return result;
        }

        /// <summary>
        /// Creates the menu items for the tray icon's context menu.
        /// </summary>
        /// <returns>An enumerable of <see cref="ToolStripMenuItem"/> objects.</returns>
        private IEnumerable<ToolStripItem> BuildMenuItems()
        {
            ToolStripMenuItem main = new ToolStripMenuItem("Open main Tetca window");
            main.Click += delegate
            {
                DoubleClick?.Invoke(this, EventArgs.Empty);
            };
            yield return main;
            yield return new ToolStripSeparator();
            yield return this.TimeDisplay = new ToolStripMenuItem()
            {
                Enabled = false
            };
            yield return new ToolStripSeparator();
            yield return this.MenuItemWithClickAction("Open today's report", this.OpenTodaysReport);
            yield return this.MenuItemWithClickAction("Test voice",
                () =>
                this.dispatcher.Invoke(() =>
                {
                    this.speech.SpeakOnSoundDevice("Testing testing");
                }));
            yield return new ToolStripSeparator();
            yield return this.MenuItemWithClickAction("Support the developer",
                () =>
                this.dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://ko-fi.com/rsheptolut",
                        UseShellExecute = true
                    });
                    return Task.CompletedTask;
                }));
            yield return this.MenuItemWithClickAction("GitHub repository",
                () =>
                this.dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/rsheptolut/tetca",
                        UseShellExecute = true
                    });
                    return Task.CompletedTask;
                }));
            yield return new ToolStripSeparator();
            yield return this.MenuItemWithClickAction("Close app",
                () =>
                this.dispatcher.Invoke(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                }));
        }

        private void OpenTodaysReport()
        {
            var path = this.workRecorder.GetCurrentReportPath();
            new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                }
            }.Start();
        }

        /// <summary>
        /// Creates a menu item with a specified click action.
        /// </summary>
        /// <param name="text">The text to display on the menu item.</param>
        /// <param name="act">The action to perform when the menu item is clicked.</param>
        /// <returns>A <see cref="ToolStripMenuItem"/> with the specified action.</returns>
        private ToolStripMenuItem MenuItemWithClickAction(string text, Action act)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(text);
            menuItem.Click += (sender, args) => act();
            return menuItem;
        }
    }
}
