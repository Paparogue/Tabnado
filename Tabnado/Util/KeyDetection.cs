using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tabnado.Util
{
    public sealed class KeyDetection
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private const string PROCESS_NAME = "ffxiv_dx11";
        private const int FOCUS_CHECK_INTERVAL_MS = 200;
        private const int COOLDOWN_MS = 50;
        private const int KEY_DOWN_MASK = 0x8000;

        private static readonly long TicksPerMillisecond = Stopwatch.Frequency / 1000;
        private readonly long focusCheckIntervalTicks;
        private readonly long cooldownTicks;

        private int currentVirtualKey;
        private bool wasKeyPressed;
        private long lastPressTime;
        private long lastFocusCheck;
        private bool lastFocusState;
        private IntPtr cachedWindowHandle;
        private int cachedProcessId;

        public KeyDetection(int initialVirtualKey = 0x09)
        {
            currentVirtualKey = initialVirtualKey;
            focusCheckIntervalTicks = FOCUS_CHECK_INTERVAL_MS * TicksPerMillisecond;
            cooldownTicks = COOLDOWN_MS * TicksPerMillisecond;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCurrentKey(int virtualKey) => currentVirtualKey = virtualKey;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetCurrentTimeTicks() => Stopwatch.GetTimestamp();

        private bool CheckFocus()
        {
            var currentWindow = GetForegroundWindow();

            if (currentWindow == cachedWindowHandle)
                return lastFocusState;

            cachedWindowHandle = currentWindow;
            GetWindowThreadProcessId(currentWindow, out cachedProcessId);

            try
            {
                using var process = Process.GetProcessById(cachedProcessId);
                return process.ProcessName.Equals(PROCESS_NAME, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsFFXIVFocused()
        {
            long currentTime = GetCurrentTimeTicks();

            if (currentTime - lastFocusCheck >= focusCheckIntervalTicks)
            {
                lastFocusState = CheckFocus();
                lastFocusCheck = currentTime;
            }

            return lastFocusState;
        }

        public bool IsKeyPressed()
        {
            if (!IsFFXIVFocused())
                return false;

            short keyState = GetAsyncKeyState(currentVirtualKey);
            bool isKeyDown = (keyState & KEY_DOWN_MASK) != 0;

            if (!isKeyDown)
            {
                wasKeyPressed = false;
                return false;
            }

            if (wasKeyPressed)
                return false;

            long currentTime = GetCurrentTimeTicks();
            if (currentTime - lastPressTime < cooldownTicks)
                return false;

            lastPressTime = currentTime;
            wasKeyPressed = true;
            return true;
        }
    }
}                   