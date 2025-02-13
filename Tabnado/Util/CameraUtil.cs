using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Enums;
using static Tabnado.Util.CameraUtil;
using Dalamud.Utility;
using Tabnado.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Collections.Specialized;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace Tabnado.Util
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
        public unsafe GroupManager* gpm;
        private float screenWidth;
        private float screenHeight;
        Vector2 screenCenter;

        public CameraUtil(IObjectTable objectTable, IGameGui gameGui, IClientState state, PluginConfig config, IPluginLog pluginLog)
        {
            this.objectTable = objectTable;
            this.gameGui = gameGui;
            this.state = state;
            this.config = config;
            this.pluginLog = pluginLog;
            gpm = GroupManager.Instance();
            screenWidth = ImGui.GetIO().DisplaySize.X;
            screenHeight = ImGui.GetIO().DisplaySize.Y;
            screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);
            var cameraManager = CameraManager.Instance();
            if (cameraManager != null)
                camera = cameraManager->CurrentCamera;
        }

        public class ScreenMonsterObject
        {
            public required ulong GameObjectId { get; set; }
            public required IGameObject? GameObject { get; set; }
            public required string NameNKind { get; set; }
            public required Vector2 ScreenPos { get; set; }
            public required float WorldDistance { get; set; }
            public required float CameraDistance { get; set; }
            public required float Angle { get; set; }
            public required float HpPercent { get; set; }
            public required bool IsTargetable { get; set; }
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private Vector2[] GetScreenEdgePoints()
        {
            int segments = config.CollissionMultiplier;
            int pointsPerEdge = segments + 1;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(Lerp(0, screenWidth, t), 0));
            }
            for (int i = 1; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(screenWidth, Lerp(0, screenHeight, t)));
            }
            for (int i = 1; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(Lerp(screenWidth, 0, t), screenHeight));
            }
            for (int i = 1; i < pointsPerEdge - 1; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(0, Lerp(screenHeight, 0, t)));
            }
            return points.ToArray();
        }

        public Vector3[] GetCameraCornerPositions(float depth = 1f)
        {
            Vector2[] screenEdgePoints = GetScreenEdgePoints();
            Vector3[] worldPoints = new Vector3[screenEdgePoints.Length];
            for (int i = 0; i < screenEdgePoints.Length; i++)
            {
                Ray ray;
                camera->ScreenPointToRay(&ray, (int)screenEdgePoints[i].X, (int)screenEdgePoints[i].Y);
                float t = depth / ray.Direction.Z;
                worldPoints[i] = ray.Origin + ray.Direction * t;
            }
            return worldPoints;
        }

        private bool IsVisibleFromAnyEdge(ICharacter npc, Vector2[] screenEdgePoints)
        {
            const float RAYCAST_TOLERANCE = 0.5f;
            bool isVisibleFromAny = false;
            bool showDebugRaycast = config.ShowDebugRaycast;
            var drawList = showDebugRaycast ? ImGui.GetForegroundDrawList() : null;
            Vector2 npcScreenPos, npcHeadScreenPos;
            bool npcInView, npcHeadInView;
            gameGui.WorldToScreen(npc.Position, out npcScreenPos, out npcInView);
            GameObject* npcObject = (GameObject*)npc.Address;
            Vector3 npcHead = new Vector3
            {
                X = npc.Position.X,
                Y = npc.Position.Y + npcObject->Height,
                Z = npc.Position.Z
            };
            gameGui.WorldToScreen(npcHead, out npcHeadScreenPos, out npcHeadInView);
            Vector3[] edgeWorldPositions = GetCameraCornerPositions();
            for (int i = 0; i < screenEdgePoints.Length; i++)
            {
                Vector3 edgeWorldPos = edgeWorldPositions[i];
                var directionToBody = Vector3.Normalize(npc.Position - edgeWorldPos);
                float distanceToBody = Vector3.Distance(edgeWorldPos, npc.Position);
                var directionToHead = Vector3.Normalize(npcHead - edgeWorldPos);
                float distanceToHead = Vector3.Distance(edgeWorldPos, npcHead);
                bool bodyVisible = false;
                bool headVisible = false;
                RaycastHit hitBody, hitHead;
                if (Collision.TryRaycastDetailed(edgeWorldPos, directionToBody, out hitBody))
                    bodyVisible = hitBody.Distance + RAYCAST_TOLERANCE >= distanceToBody;
                if (Collision.TryRaycastDetailed(edgeWorldPos, directionToHead, out hitHead))
                    headVisible = hitHead.Distance + RAYCAST_TOLERANCE >= distanceToHead;
                if (showDebugRaycast)
                {
                    DrawDebugRaycast(drawList, screenEdgePoints[i], npcScreenPos, bodyVisible, hitBody, distanceToBody);
                    DrawDebugRaycast(drawList, screenEdgePoints[i], npcHeadScreenPos, headVisible, hitHead, distanceToHead);
                }
                if (bodyVisible || headVisible)
                {
                    isVisibleFromAny = true;
                    if (!showDebugRaycast)
                        return true;
                }
            }
            if (showDebugRaycast)
            {
                if (npcInView)
                {
                    drawList.AddCircleFilled(npcScreenPos, 5f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 0.8f)));
                }
                if (npcHeadInView)
                {
                    drawList.AddCircleFilled(npcHeadScreenPos, 5f, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 1, 0.8f)));
                }
            }
            return isVisibleFromAny;
        }

        private void DrawDebugRaycast(ImDrawListPtr drawList, Vector2 originPos, Vector2 targetPos, bool isVisible, RaycastHit hit, float distance)
        {
            drawList.AddCircleFilled(originPos, 5f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));
            Vector4 lineColor = isVisible ? new Vector4(0, 1, 0, 0.5f) : new Vector4(1, 0, 0, 0.5f);
            drawList.AddLine(originPos, targetPos, ImGui.ColorConvertFloat4ToU32(lineColor), 1.0f);
            Vector2 hitScreenPos;
            if (gameGui.WorldToScreen(hit.Point, out hitScreenPos, out bool hitInView) && hitInView)
            {
                drawList.AddCircleFilled(hitScreenPos, 3f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1)));
            }
            Vector2 textPos = Vector2.Lerp(originPos, targetPos, 0.5f);
            string distanceText = $"Hit: {hit.Distance:F1}";
            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), distanceText);
        }

        public unsafe bool IsObjectFriendly(GameObject* ob)
        {
            if (ob is null) return false;
            if (gpm is null) return false;
            bool isAlliance = gpm->MainGroup.IsEntityIdInAlliance(ob->EntityId);
            bool isGroup = gpm->MainGroup.IsEntityIdInParty(ob->EntityId);
            return isAlliance || isGroup;
        }

        private void Update()
        {
            var results = new List<ScreenMonsterObject>();
            Vector2[] screenEdgePoints = GetScreenEdgePoints();
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
                        if (config.OnlyHostilePlayers && IsObjectFriendly((GameObject*)npc.Address))
                            continue;
                        if (config.OnlyBattleNPCs && obj.ObjectKind == ObjectKind.EventNpc)
                            continue;
                        if (config.OnlyVisibleObjects || config.ShowDebugRaycast)
                            if (!IsVisibleFromAnyEdge(npc, screenEdgePoints))
                                continue;

                        results.Add(new ScreenMonsterObject
                        {
                            GameObjectId = npc.GameObjectId,
                            GameObject = obj,
                            NameNKind = npc.Name.ToString() + " " + obj.ObjectKind.ToString(),
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
    }
}
