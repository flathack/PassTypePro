using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PassTypePro.Services;

public sealed class ForegroundWindowService
{
    private IntPtr _lastExternalWindow = IntPtr.Zero;

    public void CaptureCurrentExternalWindow()
    {
        var window = NativeMethods.GetForegroundWindow();
        if (window == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == 0 || processId == (uint)Environment.ProcessId)
        {
            return;
        }

        _lastExternalWindow = window;
    }

    public async Task<bool> TryRestoreLastExternalWindowAsync(int delayMs = 100)
    {
        if (_lastExternalWindow == IntPtr.Zero)
        {
            return false;
        }

        NativeMethods.ShowWindow(_lastExternalWindow, 5);
        var success = NativeMethods.SetForegroundWindow(_lastExternalWindow);

        if (success && delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        return success;
    }

    private static partial class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
