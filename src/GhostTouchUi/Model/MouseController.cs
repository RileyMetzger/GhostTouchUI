using System.Runtime.InteropServices;

namespace GhostTouchUi.Model;

/// <summary>
/// Defines a strategy for sending synthetic mouse movement to Windows.
/// </summary>
public abstract class MouseController
{
    /// <summary>
    /// Gets a controller that uses the legacy <c>mouse_event</c> API.
    /// </summary>
    public static MouseController MEventController = new EventMouseController();

    /// <summary>
    /// Gets a controller that uses the <c>SendInput</c> API.
    /// </summary>
    public static MouseController SInputController = new SendInputMouseController();

    /// <summary>
    /// Sends a relative mouse movement request.
    /// </summary>
    /// <param name="x">The horizontal delta in mickeys.</param>
    /// <param name="y">The vertical delta in mickeys.</param>
    public abstract void MoveMouse(int x, int y);
}

/// <summary>
/// Sends mouse movement using the Win32 <c>mouse_event</c> function.
/// </summary>
public class EventMouseController : MouseController
{
    private const uint MouseeventfMove = 0x0001;

    // Import the user32.dll to use the mouse_event function
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventMouseController"/> class.
    /// </summary>
    protected internal EventMouseController()
    {
    }

    /// <summary>
    /// Moves the mouse cursor by the supplied relative offsets.
    /// </summary>
    /// <param name="x">The horizontal delta in mickeys.</param>
    /// <param name="y">The vertical delta in mickeys.</param>
    public override void MoveMouse(int x, int y)
    {
        // Call mouse_event to move the mouse cursor
        mouse_event(MouseeventfMove, x, y, 0, UIntPtr.Zero);
    }
}

/// <summary>
/// Sends mouse movement using the Win32 <c>SendInput</c> function.
/// </summary>
public class SendInputMouseController : MouseController
{
    private const uint InputMouse = 0;
    private const uint MouseeventfMove = 0x0001;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendInputMouseController"/> class.
    /// </summary>
    protected internal SendInputMouseController()
    {
    }

    // Import the user32.dll to use the SendInput function
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    /// <summary>
    /// Moves the mouse cursor by the supplied relative offsets.
    /// </summary>
    /// <param name="x">The horizontal delta in mickeys.</param>
    /// <param name="y">The vertical delta in mickeys.</param>
    public override void MoveMouse(int x, int y)
    {
        Input[] inputs = new Input[1];

        inputs[0].type = InputMouse;
        inputs[0].u.mi = new Mouseinput
        {
            dx = x,
            dy = y,
            mouseData = 0,
            dwFlags = MouseeventfMove,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };

        // Call SendInput to move the mouse cursor
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
    }

    /// <summary>
    /// Represents the native <c>INPUT</c> structure used by <c>SendInput</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public InputUnion u;
    }

    /// <summary>
    /// Represents the union portion of the native <c>INPUT</c> structure.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public Mouseinput mi;
    }

    /// <summary>
    /// Represents the native <c>MOUSEINPUT</c> structure used by <c>SendInput</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Mouseinput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
