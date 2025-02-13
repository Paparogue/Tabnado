using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Tabnado.Util
{
    public class KeyDetection
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private bool wasKeyPressed = false;
        private int currentVirtualKey = 0x09;
        private long lastPressTime = 0;
        private const int COOLDOWN_MS = 50;
        private const string PROCESS_NAME = "ffxiv_dx11";

        public void SetCurrentKey(int virtualKey)
        {
            currentVirtualKey = virtualKey;
        }

        private bool IsFFXIVFocused()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            GetWindowThreadProcessId(foregroundWindow, out int processId);

            try
            {
                using Process process = Process.GetProcessById(processId);
                return process.ProcessName.Equals(PROCESS_NAME, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public bool IsKeyPressed()
        {
            if (!IsFFXIVFocused())
                return false;

            short keyState = GetAsyncKeyState(currentVirtualKey);
            bool isKeyDown = (keyState & 0x8000) != 0;
            long currentTime = Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000);
            bool isCooldownElapsed = (currentTime - lastPressTime) >= COOLDOWN_MS;
            bool isPressed = isKeyDown && !wasKeyPressed && isCooldownElapsed;

            if (isPressed)
            {
                lastPressTime = currentTime;
            }

            wasKeyPressed = isKeyDown;
            return isPressed;
        }
    }
}