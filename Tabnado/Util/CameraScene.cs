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
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Data.Parsing;
using Microsoft.VisualBasic;

namespace Tabnado.Util
{
    public unsafe class CameraScene
    {
        private readonly IObjectTable objectTable;
        private readonly IGameGui gameGui;
        private readonly IClientState state;
        private readonly PluginConfig config;
        private readonly IPluginLog log;
        private Camera* camera;
        private List<ScreenObject> screenMonsterObjects;
        private GroupManager* groupManager;
        private float screenWidth;
        private float screenHeight;
        private Vector2 screenCenter;
        private Matrix4x4 lastViewMatrix;
        const float RAYCAST_TOLERANCE = 0.1f;
        private float rotationPercentage = 0f;
        private Matrix4x4[] lastViewMatrices = new Matrix4x4[3];
        private float[] rotationPercentages = new float[3];
        private readonly uint OWNER_IS_WORLD = 3758096384;

        public CameraScene(Plugin plugin)
        {
            this.objectTable = plugin.ObjectTable;
            this.gameGui = plugin.GameGUI;
            this.state = plugin.ClientState;
            this.config = plugin.PluginConfig;
            this.log = plugin.Log;
            screenMonsterObjects = new();
        }

        public void InitManagerInstances()
        {
            groupManager = GroupManager.Instance();
            var cameraManager = CameraManager.Instance();
            if (cameraManager != null)
            {
                camera = cameraManager->CurrentCamera;
                for (int i = 0; i < 3; i++)
                {
                    lastViewMatrices[i] = camera->ViewMatrix;
                }
            }
            else
            {
                log.Error("CameraManager->CurrentCamera is null");
            }
        }

        public float GetRotationPercentage(int index)
        {
            if (index >= 0 && index < 3)
                return rotationPercentages[index];
            return 0f;
        }

        public Vector2 GetPositionFromMonitor()
        {
            screenWidth = ImGui.GetIO().DisplaySize.X;
            screenHeight = ImGui.GetIO().DisplaySize.Y;
            float normalizedX = config.MonitorX / 100f;
            float normalizedY = config.MonitorY / 100f;
            float baseX = screenWidth * normalizedX;
            float baseY = screenHeight * normalizedY;

            if (config.UseCameraLerp)
            {
                CameraEx* cam = (CameraEx*)camera;
                float currentZoom = cam->currentZoom;
                float maxZoom = cam->maxZoom;
                float minZoom = cam->minZoom;
                float cameraLerp = config.CameraLerp;
                currentZoom = Math.Clamp(currentZoom, minZoom, maxZoom);
                float cameraT = (currentZoom - minZoom) / (maxZoom - minZoom);
                baseY = TabMath.Lerp(baseY, 0, cameraT * cameraLerp);
            }

            return new Vector2(baseX, baseY);
        }

        private Vector2[] GetScreenEdgePoints()
        {
            screenCenter = GetPositionFromMonitor();

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
                points.Add(new Vector2(TabMath.Lerp(leftX, rightX, t), topY));
            }

            for (int i = 1; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(rightX, TabMath.Lerp(topY, bottomY, t)));
            }

            for (int i = 1; i < pointsPerEdge; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(TabMath.Lerp(rightX, leftX, t), bottomY));
            }

            for (int i = 1; i < pointsPerEdge - 1; i++)
            {
                float t = i / (float)segments;
                points.Add(new Vector2(leftX, TabMath.Lerp(bottomY, topY, t)));
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
            var drawList = showDebugRaycast ? ImGui.GetBackgroundDrawList() : null;

            Vector2 npcFeetScreenPos, npcHeadScreenPos;
            bool npcInView, npcHeadInView;
            GameObject* npcObject = (GameObject*)npc.Address;
            Character* npcCharacter = (Character *)npcObject;
            float npcHeadFloat = npcCharacter->ModelContainer.CalculateHeight();
            float npcFeetFloat = npcHeadFloat * 0.2f;

            Vector3 npcHead = new Vector3
            {
                X = npc.Position.X,
                Y = npc.Position.Y + npcHeadFloat,
                Z = npc.Position.Z
            };

            Vector3 npcFeet = new Vector3
            {
                X = npc.Position.X,
                Y = npc.Position.Y + npcFeetFloat,
                Z = npc.Position.Z
            };

            gameGui.WorldToScreen(npcFeet, out npcFeetScreenPos, out npcInView);
            gameGui.WorldToScreen(npcHead, out npcHeadScreenPos, out npcHeadInView);

            Vector3[] edgeWorldPositions = GetCameraCornerPositions((config.CameraDepth-1f));

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

        public bool CameraExceedsRotation(int percent, int index)
        {
            if (index < 0 || index >= 3) return false;

            Matrix4x4 currentViewMatrix = camera->ViewMatrix;
            Vector3 lastForward = new Vector3(lastViewMatrices[index].M13, lastViewMatrices[index].M23, lastViewMatrices[index].M33);
            Vector3 lastUp = new Vector3(lastViewMatrices[index].M12, lastViewMatrices[index].M22, lastViewMatrices[index].M32);

            Vector3 currentForward = new Vector3(currentViewMatrix.M13, currentViewMatrix.M23, currentViewMatrix.M33);
            Vector3 currentUp = new Vector3(currentViewMatrix.M12, currentViewMatrix.M22, currentViewMatrix.M32);

            float forwardAngle = (float)Math.Acos(Vector3.Dot(Vector3.Normalize(lastForward), Vector3.Normalize(currentForward)));
            float upAngle = (float)Math.Acos(Vector3.Dot(Vector3.Normalize(lastUp), Vector3.Normalize(currentUp)));

            rotationPercentages[index] = Math.Max(forwardAngle / (float)Math.PI, upAngle / (float)Math.PI);

            if (rotationPercentages[index] >= ((float)percent / 100f))
            {
                lastViewMatrices[index] = currentViewMatrix;
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (camera is null || groupManager is null) {
                InitManagerInstances();
                log.Error("The Camera or GroupManager was not initilized, called for it again in Update()");
            }

            var results = new List<ScreenObject>();
            Vector2[] screenEdgePoints = GetScreenEdgePoints();
            foreach (var obj in objectTable)
            {
                if ( obj is ICharacter npc && IsSanitized(npc))
                {
                    Vector2 screenPos;
                    bool inView;
                    float unitDistance = Vector3.Distance(state.LocalPlayer!.Position, npc.Position);

                    if (unitDistance > config.MaxTargetDistance)
                        continue;

                    Character* structCharacter = ((Character*)npc.Address);
                    bool isPetOrCompanion = structCharacter-> CompanionOwnerId != OWNER_IS_WORLD;
                    bool isHostile = structCharacter->IsHostile;
                    bool isNeutral = structCharacter->Battalion == 4;
                    bool isMinion = obj.ObjectKind == ObjectKind.Companion;
                    bool isTraderNPC = obj.ObjectKind == ObjectKind.EventNpc;
                    bool isPlayer = obj.ObjectKind == ObjectKind.Player;

                    if (config.OnlyAttackableObjects &&
                        (isTraderNPC || isMinion || isPetOrCompanion ||
                        (!isPlayer && !isHostile && !isNeutral) ||
                        (isPlayer && IsObjectAllianceOrGroup((GameObject*)obj.Address))))
                        continue;

                    float npcBodyY;
                    if(config.UseDistanceLerp)
                        npcBodyY = TabMath.Lerp(npc.Position.Y, npc.Position.Y + (structCharacter->ModelContainer.CalculateHeight() / 2f), TabMath.NormalizeDistance(unitDistance, config.MaxTargetDistance, config.DistanceLerp));
                    else
                        npcBodyY = npc.Position.Y;

                    Vector3 npcBody = new Vector3
                    {
                        X = npc.Position.X,
                        Y = npcBodyY,
                        Z = npc.Position.Z
                    };

                    if (gameGui.WorldToScreen(npcBody, out screenPos, out inView) && inView)
                    {
                        if (!IsVisibleFromAnyEdge(npc, screenEdgePoints) && config.OnlyVisibleObjects)
                            continue;

                        results.Add(new ScreenObject
                        {
                            GameObjectId = npc.GameObjectId,
                            GameObject = obj,
                            GlobalInfo = $"Battalion: {structCharacter->Battalion} Hostile: {isHostile} Neutral: {isNeutral} Player: {isPlayer} Name: {npc.Name} Kind: {obj.ObjectKind}",
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
                    npc.IsTargetable &&
                   !npc.Name.TextValue.IsNullOrEmpty() &&
                   !npc.IsDead &&
                   npc.CurrentHp > 0 &&
                   state.LocalPlayer != null &&
                   npc.Address != state.LocalPlayer.Address;
        }

        public void UpdateSceneList()
        {
            Update();
        }

        public List<ScreenObject> GetObjectInsideRadius(float radius)
        {
            if (screenMonsterObjects == null || screenMonsterObjects.Count == 0)
                return new List<ScreenObject>();
            return screenMonsterObjects
                .Where(monster => monster.CameraDistance <= radius)
                .OrderBy(monster => monster.CameraDistance)
                .ToList();
        }

        public ScreenObject? GetClosestEnemyInCircle()
        {
            if (screenMonsterObjects == null || screenMonsterObjects.Count == 0)
                return null;
            return screenMonsterObjects
                .Where(monster => monster.CameraDistance <= config.CameraRadius)
                .MinBy(m => m.CameraDistance);
        }
    }
}
