using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabnado.Util
{
    internal class TabMath
    {
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
        public static float NormalizeDistance(float currentDistance, float maxDistance, float curve = 1.0f)
        {
            float clampedDistance = Math.Min(Math.Max(currentDistance, 0), maxDistance);
            float normalized = 1 - (clampedDistance / maxDistance);
            return (float)Math.Pow(normalized, curve);
        }
    }
}
