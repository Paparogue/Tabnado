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
        public required string GlobalInfo { get; set; }
        public required Vector2 ScreenPos { get; set; }
        public required float WorldDistance { get; set; }
        public required float CameraDistance { get; set; }
        public string Name { get; set; } = "";
        public string ObjectType { get; set; } = "";
        public bool IsHostile { get; set; }
        public bool IsNeutral { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsPet { get; set; }
        public int Battalion { get; set; }
    }
}