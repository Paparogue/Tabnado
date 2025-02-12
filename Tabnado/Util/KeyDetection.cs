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

        private bool wasKeyPressed = false;
        private int currentVirtualKey = 0x09;
        private long lastPressTime = 0;
        private const int COOLDOWN_MS = 50;

        public void SetCurrentKey(int virtualKey)
        {
            currentVirtualKey = virtualKey;
        }

        public bool IsKeyPressed()
        {
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