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
        [FieldOffset(0x104)] public float currentZoom;
        [FieldOffset(0x108)] public float minZoom;
        [FieldOffset(0x10C)] public float maxZoom;
    }
}