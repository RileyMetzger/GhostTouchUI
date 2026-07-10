using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GhostTouchUi.Model;

namespace GhostTouchUi.ViewModel;

/// <summary>
/// Exposes UI state and commands for the main window.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private const int MaxLogCount = 20;

    private readonly object _toggleLock = new();
    private readonly PseudoInputMonitor _pseudoInputMonitor;
    private bool _isGhosting;
    private bool _canToggleAnimation;

    /// <summary>
    /// Initializes the view model and its monitoring dependencies.
    /// </summary>
    public MainWindowViewModel()
    {
        Logs = new ObservableCollection<MouseLog>();
        CanToggleAnimation = true;
        ToggleAnimationCommand = new RelayCommand(ToggleGhosting, () => CanToggleAnimation);

        _pseudoInputMonitor = new PseudoInputMonitor(AddLog);
    }

    /// <summary>
    /// Occurs when a bindable property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the command that starts or stops ghosting.
    /// </summary>
    public ICommand ToggleAnimationCommand { get; }

    /// <summary>
    /// Gets the rolling activity log shown in the UI.
    /// </summary>
    public ObservableCollection<MouseLog> Logs { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the application is actively ghosting.
    /// </summary>
    public bool IsGhosting
    {
        get => _isGhosting;
        set
        {
            if (SetField(ref _isGhosting, value))
            {
                OnPropertyChanged(nameof(CurrentGhostIcon));
            }
        }
    }

    /// <summary>
    /// Gets the icon path that represents the current ghosting state.
    /// </summary>
    public string CurrentGhostIcon => !IsGhosting ?
        "Assets/Icons/Chibi_ghost_white.ico" :
        "Assets/Icons/Chibi_ghost_purple.ico";

    /// <summary>
    /// Gets or sets a value indicating whether the toggle command can execute.
    /// </summary>
    public bool CanToggleAnimation
    {
        get => _canToggleAnimation;
        set
        {
            SetField(ref _canToggleAnimation, value);
        }
    }

    /// <summary>
    /// Reserved hook for reacting to log collection changes.
    /// </summary>
    /// <param name="sender">The collection that changed.</param>
    /// <param name="e">Details about the collection mutation.</param>
    private void Logs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Here we will notify the view when the collection has changed
        // No need to create a custom event; the view can access Logs directly.
    }

    /// <summary>
    /// Appends a log entry on the UI thread and trims the log to its maximum size.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="isActivation">True when the log item represents a start or stop event.</param>
    private void AddLog(string message, bool isActivation)
    {
        // Use the Dispatcher to ensure the code runs on the UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            lock (Logs)
            {
                Logs.Add(new MouseLog
                {
                    IsActivationEvent = isActivation,
                    Message = message,
                    Timestamp = DateTime.Now
                });

                if (Logs.Count > MaxLogCount)
                {
                    Logs.RemoveAt(0);
                }
            }
        });
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="propertyName">The property name. This is supplied automatically for callers.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Updates a backing field and raises a notification when the value changes.
    /// </summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="field">The storage location for the property value.</param>
    /// <param name="value">The new value to assign.</param>
    /// <param name="propertyName">The property name. This is supplied automatically for callers.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise, <see langword="false"/>.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Toggles ghosting on or off and starts or stops the input monitor accordingly.
    /// </summary>
    private void ToggleGhosting()
    {
        lock (_toggleLock)
        {
            IsGhosting = !IsGhosting;

            if (IsGhosting)
            {
                _pseudoInputMonitor.StartMonitoring();
            }
            else
            {
                _pseudoInputMonitor.StopMonitoring();
            }
        }
    }

    /// <summary>
    /// Releases the monitor used by the view model.
    /// </summary>
    public void Dispose()
    {
        _pseudoInputMonitor.Dispose();
    }
}

/// <summary>
/// Provides a lightweight <see cref="ICommand"/> implementation backed by delegates.
/// </summary>
public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;
    private EventHandler? _canExecuteChanged;

    /// <summary>
    /// Occurs when the command's executable state changes.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
            _canExecuteChanged += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
            _canExecuteChanged -= value;
        }
    }

    /// <summary>
    /// Determines whether the command can currently execute.
    /// </summary>
    /// <param name="parameter">Optional command parameter supplied by WPF.</param>
    /// <returns><see langword="true"/> when the command is allowed to execute; otherwise, <see langword="false"/>.</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>
    /// Executes the command action.
    /// </summary>
    /// <param name="parameter">Optional command parameter supplied by WPF.</param>
    public void Execute(object? parameter) => _execute.Invoke();

    /// <summary>
    /// Notifies WPF that the command's executable state should be re-evaluated.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        _canExecuteChanged?.Invoke(this, EventArgs.Empty);

        CommandManager.InvalidateRequerySuggested();
    }
}
