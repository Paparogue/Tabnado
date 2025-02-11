using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Enums;
using static Tabnado.Objects.Camera2Enemy;
using Dalamud.Utility;
using Tabnado.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using ABI.Windows.ApplicationModel.Activation;


namespace Tabnado.Objects
{
    public class Camera2Enemy
    {
        private readonly IObjectTable objectTable;
        private readonly IGameGui gameGui;
        private readonly IClientState state;
        private readonly PluginConfig pluginConfig;
        private readonly IPluginLog pluginLog;
        private List<ScreenMonsterObject> screenMonsterObjects;

        public Camera2Enemy(IObjectTable objectTable, IGameGui gameGui, IClientState state, PluginConfig pluginConfig, IPluginLog pluginLog)
        {
            this.objectTable = objectTable;
            this.gameGui = gameGui;
            this.state = state;
            this.pluginConfig = pluginConfig;
            this.pluginLog = pluginLog;
        }

        public class ScreenMonsterObject
        {
            public required ulong GameObjectId { get; set; }
            public required IGameObject? GameObject { get; set; }
            public required string Name { get; set; }
            public required Vector2 ScreenPos { get; set; }
            public required float Distance { get; set; }
            public required float Angle { get; set; }
            public required float HpPercent { get; set; }
            public required bool IsTargetable { get; set; }

        }

        private void Update()
        {
            var results = new List<ScreenMonsterObject>();
            var screenCenter = new Vector2(ImGui.GetIO().DisplaySize.X / 2, ImGui.GetIO().DisplaySize.Y / 2);
            const float RAYCAST_TOLERANCE = 0.5f; // Adjust this value based on your game's scale

            foreach (var obj in objectTable)
            {
                if (obj is ICharacter npc && IsSanitized(npc))
                {
                    Vector2 screenPos;
                    bool inView;

                    if (gameGui.WorldToScreen(npc.Position, out screenPos, out inView) && inView)
                    {
                        if (pluginConfig.OnlyVisibleObjects && state.LocalPlayer != null)
                        {
                            var direction = Vector3.Normalize(npc.Position - state.LocalPlayer.Position);
                            RaycastHit hit;
                            float distanceToNpc = Vector3.Distance(state.LocalPlayer.Position, npc.Position);

                            if (Collision.TryRaycastDetailed(state.LocalPlayer.Position, direction, out hit))
                            {
                                if (hit.Distance + RAYCAST_TOLERANCE < distanceToNpc)
                                    continue;
                            }
                        }

                        results.Add(new ScreenMonsterObject
                        {
                            GameObjectId = npc.GameObjectId,
                            GameObject = obj,
                            Name = npc.Name.ToString(),
                            ScreenPos = screenPos,
                            Distance = Vector2.Distance(screenCenter, screenPos),
                            Angle = (float)Math.Atan2(screenPos.Y - screenCenter.Y, screenPos.X - screenCenter.X),
                            HpPercent = npc.CurrentHp / (float)npc.MaxHp * 100f,
                            IsTargetable = npc.IsTargetable
                        });
                    }
                }
            }
            screenMonsterObjects = results;
        }

        private bool IsSanitized(ICharacter npc)
        {
            return npc != null &&
                   npc.IsValid() &&
                   npc.CurrentHp > 0 &&
                   !npc.Name.TextValue.IsNullOrEmpty() &&
                   !npc.IsDead &&
                   state.LocalPlayer is not null &&
                   npc.Address != state.LocalPlayer.Address &&
                   npc.IsTargetable;
        }

        public void UpdateEnemyList()
        {
            Update();
        }

        public List<ScreenMonsterObject> GetFullEnemyList()
        {
            return screenMonsterObjects;
        }
        public ScreenMonsterObject? GetClosestCameraEnemy()
        {
            var monsters = screenMonsterObjects;
            if (monsters == null || monsters.Count == 0) return null;
            return monsters.MinBy(m => m.Distance);
        }
        public List<ScreenMonsterObject> GetEnemiesWithinCameraRadius(float radius)
        {
            if (screenMonsterObjects == null || screenMonsterObjects.Count == 0)
                return new List<ScreenMonsterObject>();

            return screenMonsterObjects
                .Where(monster => monster.Distance <= radius)
                .OrderBy(monster => monster.Distance)
                .ToList();
        }

        public ScreenMonsterObject? GetClosestEnemyExcluding(List<ScreenMonsterObject> excludeList)
        {
            if (screenMonsterObjects == null || screenMonsterObjects.Count == 0)
                return null;

            var excludeSet = new HashSet<ulong>(excludeList.Select(m => m.GameObjectId));

            return screenMonsterObjects
                .Where(monster => !excludeSet.Contains(monster.GameObjectId))
                .MinBy(m => m.Distance);
        }
    }
}