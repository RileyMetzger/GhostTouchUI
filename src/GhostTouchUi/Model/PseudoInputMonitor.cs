using System.Runtime.InteropServices;

namespace GhostTouchUi.Model;

/// <summary>
/// Monitors real user activity and sends synthetic mouse movement after a period of inactivity.
/// </summary>
public class PseudoInputMonitor : IDisposable
{
    // Constants
    private const int WmMousemove = 0x0200;
    private const int WmKeydown = 0x0100;
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;

    private readonly RandomMessageGenerator _messageGenerator;
    private readonly object _lock = new();
    private readonly LowLevelProc _keyboardProc;
    private readonly LowLevelProc _mouseProc;
    private readonly Action<string, bool> _logger;
    private readonly int _inactivityThreshold = 45000;
    private int TimerMs { get; } = 5000;
    private MouseController InputController { get; set; }
    private RandomTimer? _timer;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private DateTime _lastInputTime;
    private volatile bool _isSendingSyntheticInput;

    /// <summary>
    /// Initializes a new instance of the <see cref="PseudoInputMonitor"/> class.
    /// </summary>
    /// <param name="logger">Callback used to record status and activity messages.</param>
    public PseudoInputMonitor(Action<string, bool> logger)
    {
        _messageGenerator = new RandomMessageGenerator(
            "Moaning",
            "Rattling Chains",
            "Clawing at the Windows",
            "Writing on the mirrors",
            "Footsteps in the hallway",
            "Whispering in the walls",
            "Tapping on the pipes",
            "Flickering the lights",
            "Shifting shadows",
            "Opening creaky doors",
            "Cold breeze through the room",
            "Scratching in the attic",
            "Moving objects slightly",
            "Knocking on the walls",
            "Breathing in your ear",
            "Curtains rustling",
            "Muffled laughter in the distance",
            "Keys jangling",
            "Cabinet doors slamming shut",
            "Humming a tune softly",
            "Chilling touch on your shoulder",
            "Sighing in the basement",
            "The floor creaking under invisible weight",
            "Turning pages of a forgotten book");

        InputController = MouseController.MEventController;
        _logger = logger;

        // Initialize hook procedures
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    /// <summary>
    /// Gets a value indicating whether input monitoring is active.
    /// </summary>
    public bool IsMonitoring { get; private set; }

    /// <summary>
    /// Starts monitoring for keyboard and mouse activity and enables the inactivity timer.
    /// </summary>
    public void StartMonitoring()
    {
        if (IsMonitoring)
        {
            return;
        }

        _logger("Ghosting activated", true);

        IsMonitoring = true;
        _lastInputTime = DateTime.Now;

        // Set hooks
        _keyboardHookId = SetHook(_keyboardProc, WhKeyboardLl);
        _mouseHookId = SetHook(_mouseProc, WhMouseLl);

        // Start timer
        StartTimer();
    }

    /// <summary>
    /// Stops monitoring and removes any installed hooks and timers.
    /// </summary>
    public void StopMonitoring()
    {
        if (!IsMonitoring)
        {
            return;
        }

        _logger("Ghosting stopped", true);

        IsMonitoring = false;

        // Unhook
        UnhookWindowsHookEx(_keyboardHookId);
        UnhookWindowsHookEx(_mouseHookId);
        _keyboardHookId = IntPtr.Zero;
        _mouseHookId = IntPtr.Zero;

        // Stop timer
        StopTimer();
    }

    /// <summary>
    /// Starts the randomized inactivity polling timer.
    /// </summary>
    private void StartTimer()
    {
        if (_timer == null)
        {
            _timer = new RandomTimer(TimerCallBackMethod, null!, TimerMs, TimerMs + 5000);
        }
    }

    /// <summary>
    /// Stops and disposes the inactivity polling timer.
    /// </summary>
    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Checks whether the inactivity threshold has been exceeded and injects synthetic input if needed.
    /// </summary>
    /// <param name="state">Unused timer state value.</param>
    private void TimerCallBackMethod(object? state)
    {
        DateTime lastInputTimeCopy;
        lock (_lock)
        {
            lastInputTimeCopy = _lastInputTime;
        }

        if ((DateTime.Now - lastInputTimeCopy).TotalMilliseconds > _inactivityThreshold)
        {
            SendFakeMouseSignal();
        }
    }

    /// <summary>
    /// Sends a minimal mouse movement to simulate activity without materially changing cursor position.
    /// </summary>
    private void SendFakeMouseSignal()
    {
        _isSendingSyntheticInput = true;
        InputController.MoveMouse(1, 0);
        InputController.MoveMouse(-1, 0);
        _isSendingSyntheticInput = false;
        _logger(_messageGenerator.Get(), false);
    }

    #region Input Hook Methods

    /// <summary>
    /// Represents a low-level Windows hook procedure.
    /// </summary>
    /// <param name="nCode">A code indicating how the hook procedure should process the message.</param>
    /// <param name="wParam">Additional message-specific information.</param>
    /// <param name="lParam">A pointer to message-specific data.</param>
    /// <returns>The result returned to the next hook procedure in the chain.</returns>
    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Handles low-level keyboard messages and refreshes the last real input timestamp.
    /// </summary>
    /// <param name="nCode">A code indicating how the hook procedure should process the message.</param>
    /// <param name="wParam">The keyboard message identifier.</param>
    /// <param name="lParam">A pointer to the keyboard message data.</param>
    /// <returns>The value returned by the next hook in the chain.</returns>
    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        bool isSendingSyntheticInputCopy = _isSendingSyntheticInput;
        if (nCode >= 0 && wParam == (IntPtr)WmKeydown && !isSendingSyntheticInputCopy)
        {
            lock (_lock)
            {
                _lastInputTime = DateTime.Now;
            }
        }

        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    /// <summary>
    /// Handles low-level mouse movement messages and refreshes the last real input timestamp.
    /// </summary>
    /// <param name="nCode">A code indicating how the hook procedure should process the message.</param>
    /// <param name="wParam">The mouse message identifier.</param>
    /// <param name="lParam">A pointer to the mouse message data.</param>
    /// <returns>The value returned by the next hook in the chain.</returns>
    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        bool isSendingSyntheticInputCopy = _isSendingSyntheticInput;
        if (nCode >= 0 && wParam == (IntPtr)WmMousemove && !isSendingSyntheticInputCopy)
        {
            lock (_lock)
            {
                _lastInputTime = DateTime.Now;
            }
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    /// <summary>
    /// Installs a low-level Windows hook for the specified input type.
    /// </summary>
    /// <param name="proc">The hook callback to install.</param>
    /// <param name="hookType">The Windows hook type identifier.</param>
    /// <returns>A handle to the installed hook.</returns>
    private IntPtr SetHook(LowLevelProc proc, int hookType)
    {
        IntPtr hInstance = GetModuleHandle(null);
        return SetWindowsHookEx(hookType, proc, hInstance, 0);
    }

    /// <summary>
    /// Installs an application-defined hook procedure into a hook chain.
    /// </summary>
    /// <param name="idHook">The type of hook procedure to install.</param>
    /// <param name="lpfn">The callback that will handle hook notifications.</param>
    /// <param name="hMod">A handle to the module containing the hook procedure.</param>
    /// <param name="dwThreadId">The thread identifier to associate with the hook, or zero for all threads.</param>
    /// <returns>A handle to the installed hook, or <see cref="IntPtr.Zero"/> on failure.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    /// <summary>
    /// Removes a hook procedure previously installed in a hook chain.
    /// </summary>
    /// <param name="hhk">The handle to the hook to remove.</param>
    /// <returns><see langword="true"/> if the hook is removed; otherwise, <see langword="false"/>.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    /// Passes hook information to the next hook procedure in the current chain.
    /// </summary>
    /// <param name="hhk">The handle to the current hook.</param>
    /// <param name="nCode">The hook code passed to the current hook procedure.</param>
    /// <param name="wParam">The message parameter passed to the current hook procedure.</param>
    /// <param name="lParam">The data parameter passed to the current hook procedure.</param>
    /// <returns>The value returned by the next hook procedure in the chain.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Retrieves a module handle for the specified module name.
    /// </summary>
    /// <param name="lpModuleName">The module name, or <see langword="null"/> for the current process executable.</param>
    /// <returns>A handle to the requested module.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    #endregion

    /// <summary>
    /// Stops monitoring and releases unmanaged hook-related resources.
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();
    }
}
