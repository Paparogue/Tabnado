using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
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
        private List<ScreenObject> screenObjectList;
        private GroupManager* groupManager;
        private float screenWidth;
        private float screenHeight;
        private Vector2 screenCenter;
        private Matrix4x4 lastViewMatrix;
        const float RAYCAST_TOLERANCE = 0.2f;
        private float rotationPercentage = 0f;
        private Matrix4x4[] lastViewMatrices = new Matrix4x4[3];
        private float[] rotationPercentages = new float[3];
        private readonly uint OWNER_IS_WORLD = 0xE0000000;
        private bool isInitialized = false;
        private bool initializationAttempted = false;

        private class RaycastDebugData
        {
            public Vector2 Origin { get; set; }
            public Vector2 Target { get; set; }
            public bool IsVisible { get; set; }
            public float HitDistance { get; set; }
            public float ActualDistance { get; set; }
        }

        private class NPCDebugData
        {
            public Vector2 FeetScreenPos { get; set; }
            public Vector2 HeadScreenPos { get; set; }
            public bool FeetInView { get; set; }
            public bool HeadInView { get; set; }
            public float VisibilityPercentage { get; set; }
            public List<RaycastDebugData> Raycasts { get; set; } = new List<RaycastDebugData>();
        }

        private readonly Dictionary<ulong, NPCDebugData> debugDataCache = new Dictionary<ulong, NPCDebugData>();
        private readonly object debugDataLock = new object();

        public CameraScene(Plugin plugin)
        {
            this.objectTable = plugin.ObjectTable;
            this.gameGui = plugin.GameGUI;
            this.state = plugin.ClientState;
            this.config = plugin.PluginConfig;
            this.log = plugin.Log;
            screenObjectList = new();
        }

        public GroupManager* GetGroupManager()
        {
            EnsureInitialized();
            return groupManager;
        }

        public Camera* GetCamera()
        {
            EnsureInitialized();
            return camera;
        }

        private bool TryInitialize()
        {
            try
            {
                groupManager = GroupManager.Instance();
                if (groupManager == null)
                {
                    log.Verbose("GroupManager not ready yet");
                    return false;
                }

                var cameraManager = CameraManager.Instance();
                if (cameraManager == null)
                {
                    log.Verbose("CameraManager not ready yet");
                    return false;
                }

                camera = cameraManager->CurrentCamera;
                if (camera == null)
                {
                    log.Verbose("CurrentCamera not ready yet");
                    return false;
                }

                var viewMatrix = camera->ViewMatrix;
                if (viewMatrix.Equals(Matrix4x4.Identity) || float.IsNaN(viewMatrix.M11))
                {
                    log.Verbose("Camera matrices not properly initialized yet");
                    return false;
                }

                for (int i = 0; i < 3; i++)
                {
                    lastViewMatrices[i] = viewMatrix;
                }

                isInitialized = true;
                log.Information("CameraScene successfully initialized");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Failed to initialize CameraScene: {ex}");
                return false;
            }
        }

        private void EnsureInitialized()
        {
            if (!isInitialized && !initializationAttempted)
            {
                initializationAttempted = true;
                if (!TryInitialize())
                {
                    log.Warning("Initial CameraScene initialization failed, will retry on next access");
                }
            }
            else if (!isInitialized)
            {
                TryInitialize();
            }
        }

        public Vector2 GetPositionFromMonitor()
        {
            screenWidth = ImGui.GetIO().DisplaySize.X;
            screenHeight = ImGui.GetIO().DisplaySize.Y;
            float normalizedX = config.MonitorX / 100f;
            float normalizedY = config.MonitorY / 100f;
            float baseX = screenWidth * normalizedX;
            float baseY = screenHeight * normalizedY;

            if (config.UseCameraLerp && isInitialized && camera != null)
            {
                //log.Information($"Camera pointer address: 0x{(IntPtr)camera:X}");
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
            EnsureInitialized();
            if (!isInitialized || camera == null)
            {
                log.Warning("Cannot get camera corner positions - camera not initialized");
                return Array.Empty<Vector3>();
            }

            Vector2[] screenEdgePoints = GetScreenEdgePoints();
            Vector3[] worldPoints = new Vector3[screenEdgePoints.Length];
            for (int i = 0; i < screenEdgePoints.Length; i++)
            {
                Ray ray;
                camera->ScreenPointToRay(&ray, (int)screenEdgePoints[i].X, (int)screenEdgePoints[i].Y);

                if (Math.Abs(ray.Direction.Z) < 0.0001f)
                {
                    log.Warning($"Invalid ray direction at point {i}: {ray.Direction}");
                    worldPoints[i] = ray.Origin + ray.Direction * depth;
                }
                else
                {
                    float t = depth / ray.Direction.Z;
                    worldPoints[i] = ray.Origin + ray.Direction * t;
                }
            }
            return worldPoints;
        }

        private bool IsVisibleFromAnyEdge(ICharacter npc, Vector2[] screenEdgePoints)
        {
            if (!isInitialized || camera == null)
                return true;

            int successfulRays = 0;
            int totalRays = screenEdgePoints.Length * 2;
            bool collectDebugData = config.ShowDebugRaycast;

            NPCDebugData debugData = null!;
            if (collectDebugData)
            {
                debugData = new NPCDebugData();
            }

            Vector2 npcFeetScreenPos, npcHeadScreenPos;
            bool npcInView, npcHeadInView;
            GameObject* npcObject = (GameObject*)npc.Address;
            Character* npcCharacter = (Character*)npcObject;
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

            if (collectDebugData)
            {
                debugData!.FeetScreenPos = npcFeetScreenPos;
                debugData!.HeadScreenPos = npcHeadScreenPos;
                debugData!.FeetInView = npcInView;
                debugData!.HeadInView = npcHeadInView;
            }

            Vector3[] edgeWorldPositions = GetCameraCornerPositions((config.CameraDepth - 1f));
            if (edgeWorldPositions.Length == 0)
                return true;

            float requiredPercentage = config.VisibilityPercent / 100f;
            int requiredSuccessCount = (int)Math.Ceiling(requiredPercentage * totalRays);

            bool RaycastAndCollectData(Vector3 edgeWorldPos, Vector3 target, Vector2 screenEdgePoint, Vector2 targetScreenPos)
            {
                Vector3 delta = target - edgeWorldPos;
                float distance = delta.Length();
                if (distance <= 0f)
                    return false;
                Vector3 direction = delta / distance;
                Collision.TryRaycastDetailed(edgeWorldPos, direction, out RaycastHit hit);
                bool isVisible = hit.Distance + RAYCAST_TOLERANCE >= distance;

                if (collectDebugData)
                {
                    debugData!.Raycasts.Add(new RaycastDebugData
                    {
                        Origin = screenEdgePoint,
                        Target = targetScreenPos,
                        IsVisible = isVisible,
                        HitDistance = hit.Distance,
                        ActualDistance = distance
                    });
                }

                return isVisible;
            }

            for (int i = 0; i < screenEdgePoints.Length && i < edgeWorldPositions.Length; i++)
            {
                Vector3 edgeWorldPos = edgeWorldPositions[i];

                if (RaycastAndCollectData(edgeWorldPos, npcFeet, screenEdgePoints[i], npcFeetScreenPos))
                    successfulRays++;
                if (RaycastAndCollectData(edgeWorldPos, npcHead, screenEdgePoints[i], npcHeadScreenPos))
                    successfulRays++;

                if (!collectDebugData)
                {
                    int raysDone = (i + 1) * 2;
                    int raysLeft = totalRays - raysDone;
                    if (successfulRays >= requiredSuccessCount)
                        return true;
                    if (successfulRays + raysLeft < requiredSuccessCount)
                        return false;
                }
            }

            float visibilityPercentage = (successfulRays * 100f) / totalRays;

            if (collectDebugData)
            {
                debugData!.VisibilityPercentage = visibilityPercentage;
                lock (debugDataLock)
                {
                    debugDataCache[npc.GameObjectId] = debugData;
                }
            }

            return (successfulRays / (float)totalRays) >= requiredPercentage;
        }

        public void DrawDebugRaycast()
        {
            if (!config.ShowDebugRaycast)
                return;

            var drawList = ImGui.GetBackgroundDrawList();

            lock (debugDataLock)
            {
                foreach (var kvp in debugDataCache)
                {
                    var debugData = kvp.Value;

                    foreach (var raycast in debugData.Raycasts)
                    {
                        drawList.AddCircleFilled(raycast.Origin, 5f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));
                        Vector4 lineColor = raycast.IsVisible ? new Vector4(0, 1, 0, 0.5f) : new Vector4(1, 0, 0, 0.5f);
                        drawList.AddLine(raycast.Origin, raycast.Target, ImGui.ColorConvertFloat4ToU32(lineColor), 1.0f);
                    }

                    if (debugData.FeetInView)
                    {
                        drawList.AddCircleFilled(debugData.FeetScreenPos, 5f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 0.8f)));
                    }
                    if (debugData.HeadInView)
                    {
                        drawList.AddCircleFilled(debugData.HeadScreenPos, 5f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 1, 0.8f)));
                    }

                    Vector2 textPos = new Vector2(debugData.FeetScreenPos.X, debugData.FeetScreenPos.Y - 20);
                    string percentageText = $"{debugData.VisibilityPercentage:F1}%";
                    drawList.AddText(textPos,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), percentageText);
                }
            }
        }

        public void ClearDebugData()
        {
            lock (debugDataLock)
            {
                debugDataCache.Clear();
            }
        }

        public unsafe bool IsPlayerGroupOrAlliance(GameObject* ob)
        {
            if (ob is null) return false;
            EnsureInitialized();
            if (!isInitialized || groupManager == null) return false;

            bool isAlliance = groupManager->MainGroup.IsEntityIdInAlliance(ob->EntityId);
            bool isGroup = groupManager->MainGroup.IsEntityIdInParty(ob->EntityId);
            return isAlliance || isGroup;
        }

        public bool UpdateMatrice(int index)
        {
            if (index < 0 || index >= 3) return false;
            EnsureInitialized();
            if (!isInitialized || camera == null) return false;

            lastViewMatrices[index] = camera->ViewMatrix;
            return true;
        }

        public bool CameraExceedsRotation(int percent, int index, bool updateMatrice)
        {
            if (index < 0 || index >= 3) return false;
            EnsureInitialized();
            if (!isInitialized || camera == null) return false;

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
                if (updateMatrice)
                    lastViewMatrices[index] = currentViewMatrix;
                return true;
            }

            return false;
        }

        public float GetRotationPercentage(int index)
        {
            if (index >= 0 && index < 3)
                return rotationPercentages[index];
            return 0f;
        }

        public void UpdateSceneList()
        {
            EnsureInitialized();
            if (!isInitialized)
            {
                log.Verbose("Skipping UpdateSceneList - not initialized");
                screenObjectList = new List<ScreenObject>();
                return;
            }

            if (config.ShowDebugRaycast)
            {
                ClearDebugData();
            }

            var results = new List<ScreenObject>();
            Vector2[] screenEdgePoints = GetScreenEdgePoints();
            foreach (var obj in objectTable)
            {
                if (obj is ICharacter npc && IsSanitized(npc))
                {
                    Vector2 screenPos;
                    bool inView;
                    float unitDistance = Vector3.Distance(state.LocalPlayer!.Position, npc.Position);

                    if (unitDistance > config.MaxTargetDistance)
                        continue;

                    Character* structCharacter = ((Character*)npc.Address);
                    bool isHostile = structCharacter->IsHostile;
                    bool isNeutral = structCharacter->Battalion == 4;
                    bool isPetOrCompanion = structCharacter->CompanionOwnerId != OWNER_IS_WORLD && !isNeutral;
                    bool isMinion = obj.ObjectKind == ObjectKind.Companion;
                    bool isTraderNPC = obj.ObjectKind == ObjectKind.EventNpc;
                    bool isPlayer = obj.ObjectKind == ObjectKind.Player;
                    bool isFriendlyPvPPlayer = state.IsPvP && isPlayer && IsPlayerGroupOrAlliance((GameObject*)obj.Address);

                    if (config.OnlyAttackableObjects &&
                        (isTraderNPC || isMinion || isPetOrCompanion ||
                        ((!isHostile && !isNeutral) && !state.IsPvP) || isFriendlyPvPPlayer))
                        continue;

                    float npcBodyY;
                    if (config.UseDistanceLerp)
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
                            GlobalInfo = $"Pet: {isPetOrCompanion} Battalion: {structCharacter->Battalion} Hostile: {isHostile} Neutral: {isNeutral} Player: {isPlayer} Name: {npc.Name} Kind: {obj.ObjectKind}",
                            ScreenPos = screenPos,
                            CameraDistance = Vector2.Distance(screenCenter, screenPos),
                            WorldDistance = unitDistance
                        });
                    }
                }
            }
            screenObjectList = results;
        }

        private bool IsSanitized(ICharacter npc)
        {
            return npc != null &&
                    npc.IsTargetable &&
                   !(npc.Name.TextValue.Length == 0) &&
                   !npc.IsDead &&
                   npc.CurrentHp > 0 &&
                   state.LocalPlayer != null &&
                   npc.Address != state.LocalPlayer.Address;
        }

        public List<ScreenObject> GetObjectInsideRadius(float radius, bool alternative = false)
        {
            if (screenObjectList == null || screenObjectList.Count == 0)
                return [];

            var objectsInRadius = screenObjectList
                .Where(o => o.CameraDistance <= radius)
                .ToList();

            if (objectsInRadius.Count == 0)
                return [];

            if (!alternative)
            {
                return objectsInRadius
                    .OrderBy(o => o.CameraDistance)
                    .ToList();
            }
            else
            {
                var closestObject = objectsInRadius
                    .OrderBy(o => o.CameraDistance)
                    .First();

                var remainingObjects = objectsInRadius
                    .Where(o => o != closestObject)
                    .OrderBy(o => Vector3.Distance(o.GameObject!.Position, closestObject.GameObject!.Position))
                    .ToList();

                var result = new List<ScreenObject> { closestObject };
                result.AddRange(remainingObjects);

                return result;
            }
        }

        public List<ScreenObject> GetObjectInsideRectangle(float width, float height, bool alternative = false)
        {
            if (screenObjectList == null || screenObjectList.Count == 0)
                return [];

            screenCenter = GetPositionFromMonitor();
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            var objectsInRectangle = screenObjectList
                .Where(o =>
                {
                    float xDist = Math.Abs(o.ScreenPos.X - screenCenter.X);
                    float yDist = Math.Abs(o.ScreenPos.Y - screenCenter.Y);
                    return xDist <= halfWidth && yDist <= halfHeight;
                })
                .ToList();

            if (objectsInRectangle.Count == 0)
                return [];

            if (!alternative)
            {
                return objectsInRectangle
                    .OrderBy(o => o.CameraDistance)
                    .ToList();
            }
            else
            {
                var closestObject = objectsInRectangle
                    .OrderBy(o => o.CameraDistance)
                    .First();

                var remainingObjects = objectsInRectangle
                    .Where(o => o != closestObject)
                    .OrderBy(o => Vector3.Distance(o.GameObject!.Position, closestObject.GameObject!.Position))
                    .ToList();

                var result = new List<ScreenObject> { closestObject };
                result.AddRange(remainingObjects);

                return result;
            }
        }

        public List<ScreenObject> GetObjectsInSelectionArea(bool alternative = false)
        {
            if (config.UseRectangleSelection)
            {
                return GetObjectInsideRectangle(config.RectangleWidth, config.RectangleHeight, alternative);
            }
            else
            {
                return GetObjectInsideRadius(config.CameraRadius, alternative);
            }
        }

        public ScreenObject? GetClosestEnemyInCircle()
        {
            if (screenObjectList == null || screenObjectList.Count == 0)
                return null;
            return screenObjectList
                .Where(monster => monster.CameraDistance <= config.CameraRadius)
                .MinBy(m => m.CameraDistance);
        }

        public ScreenObject? GetClosestEnemyInRectangle()
        {
            if (screenObjectList == null || screenObjectList.Count == 0)
                return null;

            screenCenter = GetPositionFromMonitor();
            float halfWidth = config.RectangleWidth / 2f;
            float halfHeight = config.RectangleHeight / 2f;

            return screenObjectList
                .Where(monster =>
                {
                    float xDist = Math.Abs(monster.ScreenPos.X - screenCenter.X);
                    float yDist = Math.Abs(monster.ScreenPos.Y - screenCenter.Y);
                    return xDist <= halfWidth && yDist <= halfHeight;
                })
                .MinBy(m => m.CameraDistance);
        }

        public ScreenObject? GetClosestEnemyInSelectionArea()
        {
            if (config.UseRectangleSelection)
            {
                return GetClosestEnemyInRectangle();
            }
            else
            {
                return GetClosestEnemyInCircle();
            }
        }
    }
}