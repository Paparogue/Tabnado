using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Math;
using static FFXIVClientStructs.ThisAssembly;

namespace Tabnado.Util
{
    internal static class Collision
    {
        public static bool TryRaycastDetailed(Vector3 start, Vector3 direction, out RaycastHit hitInfo, float maxDistance = 1000f, bool useSphere = false)
        {
            return useSphere
                ? BGCollisionModule.SweepSphereMaterialFilter(start, direction, out hitInfo, maxDistance)
                : BGCollisionModule.RaycastMaterialFilter(start, direction, out hitInfo, maxDistance);
        }
    }
}
