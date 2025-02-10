using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Enums;
using static BetterTarget.Objects.Camera2Enemy;


namespace BetterTarget.Objects
{
    public class Camera2Enemy
    {
        private readonly IObjectTable objectTable;
        private readonly IGameGui gameGui;
        private List<ScreenMonsterObject> screenMonsterObjects;

        public Camera2Enemy(IObjectTable objectTable, IGameGui gameGui)
        {
            this.objectTable = objectTable;
            this.gameGui = gameGui;
        }

        public class ScreenMonsterObject
        {
            public required ulong GameObjectId { get; set; }
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

            foreach (var obj in objectTable)
            {
                if (obj is ICharacter npc && IsValid(npc))
                {
                    Vector2 screenPos;
                    bool inView;
                    if (gameGui.WorldToScreen(npc.Position, out screenPos, out inView) && inView)
                    {
                        results.Add(new ScreenMonsterObject
                        {
                            GameObjectId = npc.GameObjectId,
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

        private bool IsValid(ICharacter npc)
        {
            return npc != null &&
                   npc.IsValid() &&
                   npc.CurrentHp > 0 &&
                   !npc.IsDead;
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