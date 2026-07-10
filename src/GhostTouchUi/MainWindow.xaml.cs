using System.Windows;
using GhostTouchUi.ViewModel;

namespace GhostTouchUi
{
    /// <summary>
    /// Hosts the main application user interface and coordinates UI-specific behavior.
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        /// <summary>
        /// Gets the view model currently bound to the main window.
        /// </summary>
        public static MainWindowViewModel? ViewModel { get; private set; }

        /// <summary>
        /// Initializes the main window and wires the log auto-scroll behavior.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Access the ViewModel from DataContext
            ViewModel = (MainWindowViewModel)DataContext;

            // Subscribe to the CollectionChanged event in the ViewModel's Logs collection
            ViewModel.Logs.CollectionChanged += (s, e) =>
            {
                if (GhostLogListView.Items.Count > 5)
                {
                    // Scroll to the last item in the ListView
                    GhostLogListView.ScrollIntoView(GhostLogListView.Items[GhostLogListView.Items.Count - 1]);
                }
            };
        }

        /// <summary>
        /// Hides the window when it is minimized so the application can continue from the tray.
        /// </summary>
        /// <param name="e">Event data describing the state change.</param>
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        /// <summary>
        /// Releases resources owned by the window's view model.
        /// </summary>
        public void Dispose()
        {
            ViewModel?.Dispose();
        }
    }
}
