using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GhostTouchUi.Model;

/// <summary>
/// Monitors real user activity and sends synthetic mouse movement after a period of inactivity.
/// </summary>
public class PseudoInputMonitor : IDisposable
{
    private const int WmMousemove = 0x0200;
    private const int WmKeydown = 0x0100;
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const uint LlmhfInjected = 0x00000001;

    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan MinimumCheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumCheckInterval = TimeSpan.FromSeconds(10);

    private readonly RandomMessageGenerator _messageGenerator;
    private readonly object _lock = new();
    private readonly LowLevelProc _keyboardProc;
    private readonly LowLevelProc _mouseProc;
    private readonly Action<string, bool> _logger;

    private MouseController InputController { get; }
    private RandomTimer? _timer;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private DateTime _lastInputTime;

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
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    public bool IsMonitoring { get; private set; }

    public void StartMonitoring()
    {
        if (IsMonitoring)
        {
            return;
        }

        _keyboardHookId = SetHook(_keyboardProc, WhKeyboardLl);
        if (_keyboardHookId == IntPtr.Zero)
        {
            throw CreateHookException("keyboard");
        }

        _mouseHookId = SetHook(_mouseProc, WhMouseLl);
        if (_mouseHookId == IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
            throw CreateHookException("mouse");
        }

        IsMonitoring = true;
        _lastInputTime = DateTime.Now;
        StartTimer();
        _logger("Ghosting activated", true);
    }

    public void StopMonitoring()
    {
        if (!IsMonitoring)
        {
            return;
        }

        IsMonitoring = false;

        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        StopTimer();
        _logger("Ghosting stopped", true);
    }

    private static Win32Exception CreateHookException(string hookName)
    {
        int errorCode = Marshal.GetLastWin32Error();
        return new Win32Exception(errorCode, $"Failed to install the low-level {hookName} hook.");
    }

    private void StartTimer()
    {
        _timer ??= new RandomTimer(
            TimerCallBackMethod,
            null!,
            (int)MinimumCheckInterval.TotalMilliseconds,
            (int)MaximumCheckInterval.TotalMilliseconds);
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void TimerCallBackMethod(object? state)
    {
        DateTime lastInputTimeCopy;
        lock (_lock)
        {
            lastInputTimeCopy = _lastInputTime;
        }

        if (DateTime.Now - lastInputTimeCopy > InactivityThreshold)
        {
            SendFakeMouseSignal();
        }
    }

    private void SendFakeMouseSignal()
    {
        InputController.MoveMouse(1, 0);
        InputController.MoveMouse(-1, 0);
        _logger(_messageGenerator.Get(), false);
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WmKeydown)
        {
            RecordRealInput();
        }

        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WmMousemove)
        {
            MsllHookStruct hookData = Marshal.PtrToStructure<MsllHookStruct>(lParam);
            bool wasInjected = (hookData.Flags & LlmhfInjected) != 0;

            if (!wasInjected)
            {
                RecordRealInput();
            }
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void RecordRealInput()
    {
        lock (_lock)
        {
            _lastInputTime = DateTime.Now;
        }
    }

    private IntPtr SetHook(LowLevelProc proc, int hookType)
    {
        IntPtr hInstance = GetModuleHandle(null);
        return SetWindowsHookEx(hookType, proc, hInstance, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsllHookStruct
    {
        public Point Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    public void Dispose()
    {
        StopMonitoring();
    }
}
