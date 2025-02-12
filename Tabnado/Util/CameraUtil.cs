using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Enums;
using static Tabnado.Objects.CameraUtil;
using Dalamud.Utility;
using Tabnado.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using ABI.Windows.ApplicationModel.Activation;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Collections.Specialized;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Tabnado.Objects
{
    public unsafe class CameraUtil
    {
        private readonly IObjectTable objectTable;
        private readonly IGameGui gameGui;
        private readonly IClientState state;
        private readonly PluginConfig config;
        private readonly IPluginLog pluginLog;
        private readonly Camera* camera;
        private List<ScreenMonsterObject> screenMonsterObjects;

        public CameraUtil(IObjectTable objectTable, IGameGui gameGui, IClientState state, PluginConfig config, IPluginLog pluginLog)
        {
            this.objectTable = objectTable;
            this.gameGui = gameGui;
            this.state = state;
            this.config = config;
            this.pluginLog = pluginLog;
            var cameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance();
            if (cameraManager != null)
                this.camera = cameraManager->CurrentCamera;
        }


        public class ScreenMonsterObject
        {
            public required ulong GameObjectId { get; set; }
            public required IGameObject? GameObject { get; set; }
            public required string Name { get; set; }
            public required Vector2 ScreenPos { get; set; }
            public required float WorldDistance { get; set; }
            public required float CameraDistance { get; set; }
            public required float Angle { get; set; }
            public required float HpPercent { get; set; }
            public required bool IsTargetable { get; set; }

        }

        public Vector3[] GetCameraCornerPositions(float depth = 1f)
        {
            Vector3[] corners = new Vector3[4];

            Vector2[] screenCorners = GetScreenCorners();

            for (int i = 0; i < 4; i++)
            {
                Ray ray;
                camera->ScreenPointToRay(&ray, (int)screenCorners[i].X, (int)screenCorners[i].Y);
                float t = depth / ray.Direction.Z;
                corners[i] = ray.Origin + ray.Direction * t;
            }

            return corners;
        }

        private Vector2[] GetScreenCorners()
        {
            float screenWidth = ImGui.GetIO().DisplaySize.X;
            float screenHeight = ImGui.GetIO().DisplaySize.Y;

            return new Vector2[]
            {
                new Vector2(0, 0),                          // top left
                new Vector2(screenWidth, 0),                // top right
                new Vector2(0, screenHeight),               // bottom left
                new Vector2(screenWidth, screenHeight)      // bottom right
            };
        }

        private bool IsVisibleFromAnyCorner(ICharacter npc, Vector2[] screenCorners)
        {
            const float RAYCAST_TOLERANCE = 1f;
            bool isVisibleFromAny = false;

            bool showDebugRaycast = config.ShowDebugRaycast;
            var drawList = showDebugRaycast ? ImGui.GetForegroundDrawList() : null;

            Vector2 npcScreenPos;
            bool npcInView;
            gameGui.WorldToScreen(npc.Position, out npcScreenPos, out npcInView);

            Vector3[] cornerWorldPositions = GetCameraCornerPositions();

            for (int i = 0; i < screenCorners.Length; i++)
            {
                Vector3 cornerWorldPos = cornerWorldPositions[i];
                var direction = Vector3.Normalize(npc.Position - cornerWorldPos);
                float distanceToNpc = Vector3.Distance(cornerWorldPos, npc.Position);

                if (Collision.TryRaycastDetailed(cornerWorldPos, direction, out RaycastHit hit))
                {
                    bool raycastReachesNpc = hit.Distance + RAYCAST_TOLERANCE >= distanceToNpc;

                    if (showDebugRaycast)
                    {
                        drawList.AddCircleFilled(
                            screenCorners[i],
                            5f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1))
                        );

                        Vector4 lineColor = raycastReachesNpc
                            ? new Vector4(0, 1, 0, 0.5f)
                            : new Vector4(1, 0, 0, 0.5f);

                        drawList.AddLine(
                            screenCorners[i],
                            npcScreenPos,
                            ImGui.ColorConvertFloat4ToU32(lineColor),
                            1.0f
                        );

                        Vector2 hitScreenPos;
                        if (gameGui.WorldToScreen(hit.Point, out hitScreenPos, out bool hitInView) && hitInView)
                        {
                            drawList.AddCircleFilled(
                                hitScreenPos,
                                3f,
                                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1))
                            );
                        }

                        Vector2 textPos = Vector2.Lerp(screenCorners[i], npcScreenPos, 0.5f);
                        string distanceText = $"Hit: {hit.Distance:F1}";
                        drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), distanceText);
                    }

                    if (raycastReachesNpc)
                    {
                        isVisibleFromAny = true;
                        if (!showDebugRaycast) return true;
                    }
                }
            }

            if (showDebugRaycast && npcInView)
            {
                drawList.AddCircleFilled(
                    npcScreenPos,
                    5f,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 0.8f))
                );
            }

            return isVisibleFromAny;
        }

        private void Update()
        {
            var results = new List<ScreenMonsterObject>();
            float screenWidth = ImGui.GetIO().DisplaySize.X;
            float screenHeight = ImGui.GetIO().DisplaySize.Y;
            var screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);
            var screenCorners = GetScreenCorners();

            foreach (var obj in objectTable)
            {
                if (obj is ICharacter npc && IsSanitized(npc) && state.LocalPlayer != null)
                {
                    Vector2 screenPos;
                    bool inView;
                    if (gameGui.WorldToScreen(npc.Position, out screenPos, out inView) && inView) 
                    {
                        float unitDistance = Vector3.Distance(state.LocalPlayer.Position, npc.Position);
                        if (unitDistance > config.MaxTargetDistance)
                            continue;

                        if (config.OnlyAttackAbles && !(npc.StatusFlags == StatusFlags.Hostile))
                            continue;

                        if ((config.OnlyVisibleObjects || config.ShowDebugRaycast))
                            if (!IsVisibleFromAnyCorner(npc, screenCorners))
                                continue;

                        results.Add(new ScreenMonsterObject
                        {
                            GameObjectId = npc.GameObjectId,
                            GameObject = obj,
                            Name = npc.Name.ToString(),
                            ScreenPos = screenPos,
                            CameraDistance = Vector2.Distance(screenCenter, screenPos),
                            WorldDistance = unitDistance,
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
            return monsters.MinBy(m => m.CameraDistance);
        }
        public List<ScreenMonsterObject> GetEnemiesWithinCameraRadius(float radius)
        {
            if (screenMonsterObjects == null || screenMonsterObjects.Count == 0)
                return new List<ScreenMonsterObject>();

            return screenMonsterObjects
                .Where(monster => monster.CameraDistance <= radius)
                .OrderBy(monster => monster.CameraDistance)
                .ToList();
        }

        public ScreenMonsterObject? GetClosestEnemyInCircle()
        {
            if (screenMonsterObjects == null || screenMonsterObjects.Count == 0)
                return null;

            return screenMonsterObjects
                .Where(monster => monster.CameraDistance <= config.CameraRadius)
                .MinBy(m => m.CameraDistance);
        }

        public ScreenMonsterObject? GetClosestEnemyExcluding(List<ScreenMonsterObject> excludeList)
        {
            if (screenMonsterObjects == null || screenMonsterObjects.Count == 0)
                return null;

            var excludeSet = new HashSet<ulong>(excludeList.Select(m => m.GameObjectId));

            return screenMonsterObjects
                .Where(monster => !excludeSet.Contains(monster.GameObjectId))
                .MinBy(m => m.CameraDistance);
        }
    }
}