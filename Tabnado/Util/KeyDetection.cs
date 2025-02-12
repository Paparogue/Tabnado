using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tabnado.Util
{
    public class KeyDetection
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private bool wasKeyPressed = false;
        private int currentVirtualKey = 0x09;

        public void SetCurrentKey(int virtualKey)
        {
            currentVirtualKey = virtualKey;
        }

        public bool IsKeyPressed()
        {
            short keyState = GetAsyncKeyState(currentVirtualKey);
            bool isPressed = (keyState & 0x8000) != 0 && !wasKeyPressed;
            wasKeyPressed = (keyState & 0x8000) != 0;
            return isPressed;
        }
    }
}
