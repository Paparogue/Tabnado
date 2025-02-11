using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tabnado.Objects
{
    internal class KeyDetector
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_TAB = 0x09;

        private bool wasTabPressed = false;

        public bool IsTabPressed()
        {
            short keyState = GetAsyncKeyState(VK_TAB);

            bool isPressed = ((keyState & 0x8000) != 0) && !wasTabPressed;

            wasTabPressed = (keyState & 0x8000) != 0;

            return isPressed;
        }
    }
}
