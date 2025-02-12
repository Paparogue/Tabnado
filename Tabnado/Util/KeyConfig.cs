using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabnado.Util
{
    public static class KeyConfig
    {
        public static readonly Dictionary<string, int> VirtualKeys = new Dictionary<string, int>
        {
            {"Tab", 0x09},
            {"Left Mouse", 0x01},
            {"Right Mouse", 0x02},
            {"Middle Mouse", 0x04},
            {"Backspace", 0x08},
            {"Enter", 0x0D},
            {"Shift", 0x10},
            {"Ctrl", 0x11},
            {"Alt", 0x12},
            {"Space", 0x20},
            {"Left Arrow", 0x25},
            {"Up Arrow", 0x26},
            {"Right Arrow", 0x27},
            {"Down Arrow", 0x28},
            {"0", 0x30},
            {"1", 0x31},
            {"2", 0x32},
            {"3", 0x33},
            {"4", 0x34},
            {"5", 0x35},
            {"6", 0x36},
            {"7", 0x37},
            {"8", 0x38},
            {"9", 0x39},
            {"A", 0x41},
            {"B", 0x42},
            {"C", 0x43},
            {"D", 0x44},
            {"E", 0x45},
            {"F", 0x46},
            {"G", 0x47},
            {"H", 0x48},
            {"I", 0x49},
            {"J", 0x4A},
            {"K", 0x4B},
            {"L", 0x4C},
            {"M", 0x4D},
            {"N", 0x4E},
            {"O", 0x4F},
            {"P", 0x50},
            {"Q", 0x51},
            {"R", 0x52},
            {"S", 0x53},
            {"T", 0x54},
            {"U", 0x55},
            {"V", 0x56},
            {"W", 0x57},
            {"X", 0x58},
            {"Y", 0x59},
            {"Z", 0x5A},
            {"F1", 0x70},
            {"F2", 0x71},
            {"F3", 0x72},
            {"F4", 0x73},
            {"F5", 0x74},
            {"F6", 0x75},
            {"F7", 0x76},
            {"F8", 0x77},
            {"F9", 0x78},
            {"F10", 0x79},
            {"F11", 0x7A},
            {"F12", 0x7B}
        };
    }
}