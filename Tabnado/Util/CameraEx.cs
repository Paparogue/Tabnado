using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tabnado.Util
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct CameraEx
    {
        [FieldOffset(0x114)] public float currentZoom;
        [FieldOffset(0x118)] public float minZoom;
        [FieldOffset(0x11C)] public float maxZoom;
    }
}