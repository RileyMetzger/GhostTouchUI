using System.Runtime.InteropServices;

namespace GhostMouse
{
    internal class Program
    {
        // Constants for detecting inactivity
        private const int CheckInterval = 1000; // Check every second
        private static int InactivityThreshold { get; set; } = Convert.ToInt32(TimeSpan.FromMinutes(5).TotalMilliseconds); // 5 minutes

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        const uint MOUSEEVENTF_MOVE = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Inactivity detection started...");

            while (true)
            {
                uint idleTime = GetIdleTime();

                if (idleTime > InactivityThreshold)
                {
                    Console.WriteLine("Inactivity detected. Simulating mouse movement...");
                    SimulateMouseMovement();
                }

                Thread.Sleep(CheckInterval);
            }
        }

        static uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            return (uint)Environment.TickCount - lastInPut.dwTime;
        }

        static void SimulateMouseMovement()
        {
            Random random = new Random();
            int xMove = random.Next(-5, 5);
            int yMove = random.Next(-5, 5);

            mouse_event(MOUSEEVENTF_MOVE, (uint)xMove, (uint)yMove, 0, 0);
        }
    }

}