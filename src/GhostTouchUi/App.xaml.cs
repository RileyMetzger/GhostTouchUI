using GhostTouchUi.ViewModel;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;

namespace GhostTouchUi
{
    /// <summary>
    /// Provides application-level startup, shutdown, and tray icon lifecycle behavior.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private TaskbarIcon? _notifyIcon;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
        }

        /// <summary>
        /// Initializes tray icon resources and event handlers when the application starts.
        /// </summary>
        /// <param name="e">Startup event data supplied by WPF.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Retrieve the TaskbarIcon from resources
            _notifyIcon = (TaskbarIcon)FindResource("AppNotifyIcon");

            // Handle double-click to restore window
            _notifyIcon.TrayMouseDoubleClick += (s, e) => Restore_OnClick(s, e);
        }

        /// <summary>
        /// Restores the main window from the notification area and ensures it is visible.
        /// </summary>
        /// <param name="sender">The UI element that raised the event.</param>
        /// <param name="e">The routed event data for the restore action.</param>
        private void Restore_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow ??= new MainWindow();
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
        }

        /// <summary>
        /// Disposes tray resources and shuts down the application.
        /// </summary>
        /// <param name="sender">The UI element that raised the event.</param>
        /// <param name="e">The routed event data for the exit action.</param>
        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            _notifyIcon?.Dispose();
            Current.Shutdown();
        }

        /// <summary>
        /// Releases application resources before the process exits.
        /// </summary>
        /// <param name="e">Exit event data supplied by WPF.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            GhostTouchUi.MainWindow.ViewModel?.Dispose();
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
