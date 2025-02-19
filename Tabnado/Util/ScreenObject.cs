using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;

namespace Tabnado.Util
{
    public class ScreenObject
    {
        public required ulong GameObjectId { get; set; }
        public required IGameObject? GameObject { get; set; }
        public required string NameNKind { get; set; }
        public required Vector2 ScreenPos { get; set; }
        public required float WorldDistance { get; set; }
        public required float CameraDistance { get; set; }
    }
}
