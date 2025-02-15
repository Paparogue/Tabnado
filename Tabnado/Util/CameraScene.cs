using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Enums;
using static Tabnado.Util.CameraScene;
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
    public unsafe class CameraScene
    {
        private readonly IObjectTable objectTable;
        private readonly IGameGui gameGui;
        private readonly IClientState state;
        private readonly PluginConfig config;
        private readonly IPluginLog pluginLog;
        private readonly Camera* camera;
        private List<ScreenMonsterObject> screenMonsterObjects;
        private GroupManager* groupManager;
        private float screenWidth;
        private float screenHeight;
        private Vector2 screenCenter;
        private Matrix4x4 lastViewMatrix;
        const float RAYCAST_TOLERANCE = 0.1f;

        public CameraScene(IObjectTable objectTable, IGameGui gameGui, IClientState state, PluginConfig config, IPluginLog pluginLog)
        {
            this.objectTable = objectTable;
            this.gameGui = gameGui;
            this.state = state;
            this.config = config;
            this.pluginLog = pluginLog;
            groupManager = GroupManager.Instance();
            screenWidth = ImGui.GetIO().DisplaySize.X;
            screenHeight = ImGui.GetIO().DisplaySize.Y;
            screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);
            var cameraManager = CameraManager.Instance();
            if (cameraManager != null)
                camera = cameraManager->CurrentCamera;
            lastViewMatrix = camera->ViewMatrix;
        }

        public class ScreenMonsterObject
        {
            public required ulong GameObjectId { get; set; }
            public required IGameObject? GameObject { get; set; }
            public required string NameNKind { get; set; }
            public required Vector2 ScreenPos { get; set; }
            public required float WorldDistance { get; set; }
            public required float CameraDistance { get; set; }
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private Vector2[] GetScreenEdgePoints()
        {
            int segments = config.RaycastMultiplier;
            int pointsPerEdge = segments + 1;
            List<Vector2> points = new List<Vector2>();

            float scaleFactor = config.RayCastPercent / 100f;
            float leftX = screenWidth * (1 - scaleFactor) / 2;
            float rightX = screenWidth * (1 + scaleFactor) / 2;
            float topY = screenHeight * (1 - scaleFactor) / 2;
            float bottomY = screenHeight * (1 + scaleFactor) / 2;

            for (int i = 0; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(Lerp(leftX, rightX, t), topY));
            }

            for (int i = 1; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(rightX, Lerp(topY, bottomY, t)));
            }

            for (int i = 1; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(Lerp(rightX, leftX, t), bottomY));
            }

            for (int i = 1; i < pointsPerEdge - 1; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(leftX, Lerp(bottomY, topY, t)));
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
            int successfulRays = 0;
            int totalRays = screenEdgePoints.Length * 2;
            bool showDebugRaycast = config.ShowDebugRaycast;
            var drawList = showDebugRaycast ? ImGui.GetForegroundDrawList() : null;

            Vector2 npcFeetScreenPos, npcHeadScreenPos;
            bool npcInView, npcHeadInView;
            GameObject* npcObject = (GameObject*)npc.Address;
            Character* npcCharacter = (Character *)npcObject;

            Vector3 npcHead = new Vector3
            {
                X = npc.Position.X,
                Y = npc.Position.Y + npcCharacter->ModelContainer.CalculateHeight(),
                Z = npc.Position.Z
            };

            Vector3 npcFeet = new Vector3
            {
                X = npc.Position.X,
                Y = npc.Position.Y + 0.5f,
                Z = npc.Position.Z
            };

            gameGui.WorldToScreen(npcFeet, out npcFeetScreenPos, out npcInView);
            gameGui.WorldToScreen(npcHead, out npcHeadScreenPos, out npcHeadInView);

            Vector3[] edgeWorldPositions = GetCameraCornerPositions();

            float requiredPercentage = config.VisibilityPercent / 100f;
            int requiredSuccessCount = (int)Math.Ceiling(requiredPercentage * totalRays);

            bool RaycastAndDraw(Vector3 edgeWorldPos, Vector3 target, Vector2 screenEdgePoint, Vector2 targetScreenPos)
            {
                Vector3 delta = target - edgeWorldPos;
                float distance = delta.Length();
                if (distance <= 0f)
                    return false;
                Vector3 direction = delta / distance;
                Collision.TryRaycastDetailed(edgeWorldPos, direction, out RaycastHit hit);
                bool isVisible = hit.Distance + RAYCAST_TOLERANCE >= distance;
                if (showDebugRaycast)
                {
                    DrawDebugRaycast(drawList, screenEdgePoint, targetScreenPos, isVisible, hit, distance);
                }
                return isVisible;
            }

            for (int i = 0; i < screenEdgePoints.Length; i++)
            {
                Vector3 edgeWorldPos = edgeWorldPositions[i];

                if (RaycastAndDraw(edgeWorldPos, npcFeet, screenEdgePoints[i], npcFeetScreenPos))
                    successfulRays++;
                if (RaycastAndDraw(edgeWorldPos, npcHead, screenEdgePoints[i], npcHeadScreenPos))
                    successfulRays++;

                if (!showDebugRaycast)
                {
                    int raysDone = (i + 1) * 2;
                    int raysLeft = totalRays - raysDone;
                    if (successfulRays >= requiredSuccessCount)
                        return true;
                    if (successfulRays + raysLeft < requiredSuccessCount)
                        return false;
                }
            }

            if (showDebugRaycast)
            {
                if (npcInView)
                {
                    drawList.AddCircleFilled(npcFeetScreenPos, 5f,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 0.8f)));
                }
                if (npcHeadInView)
                {
                    drawList.AddCircleFilled(npcHeadScreenPos, 5f,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 1, 0.8f)));
                }

                Vector2 textPos = new Vector2(npcFeetScreenPos.X, npcFeetScreenPos.Y - 20);
                float visibilityPercentage = (successfulRays * 100f) / totalRays;
                string percentageText = $"{visibilityPercentage:F1}%";
                drawList.AddText(textPos,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), percentageText);
            }

            return (successfulRays / (float)totalRays) >= requiredPercentage;
        }


        private void DrawDebugRaycast(ImDrawListPtr drawList, Vector2 originPos, Vector2 targetPos, bool isVisible, RaycastHit hit, float distance)
        {
            drawList.AddCircleFilled(originPos, 5f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));
            Vector4 lineColor = isVisible ? new Vector4(0, 1, 0, 0.5f) : new Vector4(1, 0, 0, 0.5f);
            drawList.AddLine(originPos, targetPos, ImGui.ColorConvertFloat4ToU32(lineColor), 1.0f);
        }

        public unsafe bool IsObjectAllianceOrGroup(GameObject* ob)
        {
            if (ob is null) return false;
            bool isAlliance = groupManager->MainGroup.IsEntityIdInAlliance(ob->EntityId);
            bool isGroup = groupManager->MainGroup.IsEntityIdInParty(ob->EntityId);
            return isAlliance || isGroup;
        }

        public bool CameraExceedsRotation()
        {
            Matrix4x4 currentViewMatrix = camera->ViewMatrix;
            Vector3 lastForward = new Vector3(lastViewMatrix.M13, lastViewMatrix.M23, lastViewMatrix.M33);
            Vector3 lastUp = new Vector3(lastViewMatrix.M12, lastViewMatrix.M22, lastViewMatrix.M32);

            Vector3 currentForward = new Vector3(currentViewMatrix.M13, currentViewMatrix.M23, currentViewMatrix.M33);
            Vector3 currentUp = new Vector3(currentViewMatrix.M12, currentViewMatrix.M22, currentViewMatrix.M32);

            float forwardAngle = (float)Math.Acos(Vector3.Dot(Vector3.Normalize(lastForward), Vector3.Normalize(currentForward)));
            float upAngle = (float)Math.Acos(Vector3.Dot(Vector3.Normalize(lastUp), Vector3.Normalize(currentUp)));

            float rotationPercentage = Math.Max(forwardAngle / (float)Math.PI, upAngle / (float)Math.PI);

            if (rotationPercentage >= ((float)config.RotationPercent/100f))
            {
                lastViewMatrix = currentViewMatrix;
                return true;
            }

            return false;
        }

        private void Update()
        {
            var results = new List<ScreenMonsterObject>();
            Vector2[] screenEdgePoints = GetScreenEdgePoints();
            foreach (var obj in objectTable)
            {
                if ( obj is ICharacter npc && IsSanitized(npc))
                {
                    Vector2 screenPos;
                    bool inView;
                    if (gameGui.WorldToScreen(npc.Position, out screenPos, out inView) && inView)
                    {
                        
                        float unitDistance = Vector3.Distance(state.LocalPlayer!.Position, npc.Position);
                        if (unitDistance > config.MaxTargetDistance)
                            continue;
                        if (config.OnlyHostilePlayers && IsObjectAllianceOrGroup((GameObject*)npc.Address))
                            continue;
                        if (config.OnlyBattleNPCs && obj.ObjectKind == ObjectKind.EventNpc || npc.Name.Equals("Carbuncle") || npc.Name.Equals("Eos") || npc.Name.Equals("Eos"))
                            continue;
                        if (config.OnlyVisibleObjects || config.ShowDebugRaycast)
                            if (!IsVisibleFromAnyEdge(npc, screenEdgePoints))
                                continue;

                        results.Add(new ScreenMonsterObject
                        {
                            GameObjectId = npc.GameObjectId,
                            GameObject = obj,
                            NameNKind = $"{npc.Name} {obj.ObjectKind}",
                            ScreenPos = screenPos,
                            CameraDistance = Vector2.Distance(screenCenter, screenPos),
                            WorldDistance = unitDistance
                        });
                    }
                }
            }
            screenMonsterObjects = results;
        }

        private bool IsSanitized(ICharacter npc)
        {
            return npc != null &&
                   npc.CurrentHp > 0 &&
                   !npc.Name.TextValue.IsNullOrEmpty() &&
                   !npc.IsDead &&
                   state.LocalPlayer != null &&
                   npc.Address != state.LocalPlayer.Address &&
                   npc.IsTargetable;
        }

        public void UpdateEnemyList()
        {
            Update();
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
