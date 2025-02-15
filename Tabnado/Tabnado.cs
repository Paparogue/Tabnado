using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Tabnado.UI;
using ImGuiNET;
using static FFXIVClientStructs.ThisAssembly;
using static Tabnado.Util.CameraUtil;
using Tabnado.Util;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Tabnado
{
    public unsafe class Tabnado
    {
        private IClientState clientState;
        private IObjectTable objectTable;
        private ITargetManager targetManager;
        private IChatGui chatGui;
        private PluginConfig config;
        private CameraUtil cameraUtil;
        private IGameGui gameGui;
        private IPluginLog pluginLog;
        private KeyDetection keyDetection;
        private bool wasTabPressed = false;
        private int currentEnemyIndex;
        private List<ScreenMonsterObject> lastEnemyList;
        private DateTime lastUpdateTime;
        private DateTime lastClearTime;
        private ulong previousClosestTargetId;
        private const int CIRCLE_SEGMENTS = 16;
        private Vector3[] circlePoints;
        private bool circlePointsInitialized = false;

        public Tabnado(IClientState clientState, IObjectTable objectTable, ITargetManager targetManager,
                      IChatGui chatGui, PluginConfig config, CameraUtil cameraUtil, IGameGui gameGui,
                      IPluginLog pluginLog, KeyDetection keyDetection)
        {
            this.clientState = clientState;
            this.objectTable = objectTable;
            this.targetManager = targetManager;
            this.chatGui = chatGui;
            this.config = config;
            this.cameraUtil = cameraUtil;
            this.gameGui = gameGui;
            this.pluginLog = pluginLog;
            this.keyDetection = keyDetection;
            lastUpdateTime = DateTime.Now;
            lastClearTime = DateTime.Now;
            currentEnemyIndex = -1;
            lastEnemyList = new();
            InitializeCirclePoints();
        }

        private void InitializeCirclePoints()
        {
            circlePoints = new Vector3[CIRCLE_SEGMENTS];
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float angle = (float)(2 * Math.PI * i / CIRCLE_SEGMENTS);
                circlePoints[i] = new Vector3(
                    MathF.Cos(angle),
                    0,
                    MathF.Sin(angle)
                );
            }
            circlePointsInitialized = true;
        }

        private void Draw3DSelectionCircle(Character* character)
        {
            if (!circlePointsInitialized) return;

            var cameraManager = CameraManager.Instance();
            if (cameraManager == null || cameraManager->CurrentCamera == null) return;

            float radius = character->Height * 0.5f;
            Vector3 characterPos = new(character->Position.X, character->Position.Y, character->Position.Z);

            var drawList = ImGui.GetForegroundDrawList();
            Vector2 lastScreenPos = new();
            bool lastInView = false;

            for (int i = 0; i <= CIRCLE_SEGMENTS; i++)
            {
                int index = i % CIRCLE_SEGMENTS;
                Vector3 worldPoint = new(
                    characterPos.X + circlePoints[index].X * radius,
                    characterPos.Y + 0.1f,
                    characterPos.Z + circlePoints[index].Z * radius
                );

                Vector2 screenPos;
                bool inView;
                gameGui.WorldToScreen(worldPoint, out screenPos, out inView);

                if (i > 0 && (inView || lastInView))
                {
                    float time = DateTime.Now.Millisecond / 1000f;
                    float alpha = 0.4f + MathF.Sin(time * MathF.PI * 2) * 0.2f;

                    drawList.AddLine(
                        lastScreenPos,
                        screenPos,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.7f, 0, alpha)),
                        2f
                    );
                }

                lastScreenPos = screenPos;
                lastInView = inView;
            }
        }

        public void Draw()
        {
            if (cameraUtil == null)
                return;

            List<ScreenMonsterObject> enemies = null;
            var currentTime = DateTime.Now;
            var clearTargetUpdate = config.ClearTargetTable && (currentTime - lastClearTime).TotalMilliseconds > 250;
            var refreshRateUpdate = config.DrawSelection && (currentTime - lastUpdateTime).TotalMilliseconds >= config.DrawRefreshRate;

            if (config.ShowDebugRaycast || config.ShowDebugSelection || refreshRateUpdate || clearTargetUpdate)
            {
                cameraUtil.UpdateEnemyList();
                enemies = cameraUtil.GetEnemiesWithinCameraRadius(config.CameraRadius);
                lastUpdateTime = currentTime;
                if(clearTargetUpdate && enemies.Count <= 0)
                {
                    currentEnemyIndex = -1;
                    lastEnemyList.Clear();
                    previousClosestTargetId = 0;
                    lastClearTime = currentTime;
                }
            }

            if (keyDetection.IsKeyPressed())
            {
                cameraUtil.UpdateEnemyList();
                enemies = cameraUtil.GetEnemiesWithinCameraRadius(config.CameraRadius);
                bool resetTarget = false;

                if (config.UseCameraRotationReset && cameraUtil.CameraExceedsRotation())
                {
                    resetTarget = true;
                }

                if (config.UseCombatantReset && !IsListEqual(lastEnemyList, enemies))
                {
                    resetTarget = true;
                    lastEnemyList = new List<ScreenMonsterObject>(enemies);
                }

                if (config.UseNewTargetReset && enemies.Count > 0)
                {
                    var closestEnemy = enemies[0];
                    if (closestEnemy.GameObjectId != previousClosestTargetId)
                    {
                        resetTarget = true;
                        previousClosestTargetId = closestEnemy.GameObjectId;
                    }
                }

                if (enemies.Count > 0)
                {
                    if (resetTarget)
                    {
                        currentEnemyIndex = 0;
                    }
                    else
                    {
                        currentEnemyIndex++;
                        if (currentEnemyIndex >= enemies.Count)
                            currentEnemyIndex = 0;
                    }

                    targetManager.Target = enemies[currentEnemyIndex].GameObject;
                }
            }

            if (config.ShowDebugSelection)
            {
                ShowDebugSelection();
            }
        }

        private bool IsListEqual(List<ScreenMonsterObject> list1, List<ScreenMonsterObject> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].GameObjectId != list2[i].GameObjectId)
                    return false;
            }

            return true;
        }

        private void ShowDebugSelection()
        {
            if (!config.ShowDebugSelection) return;

            var screenCenter = new Vector2(ImGui.GetIO().DisplaySize.X / 2, ImGui.GetIO().DisplaySize.Y / 2);
            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddCircleFilled(screenCenter, 3f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));

            drawList.AddCircle(
                screenCenter,
                config.CameraRadius,
                ImGui.ColorConvertFloat4ToU32(new Vector4(2, 0, 0, 1f)),
                32
            );

            var enemies = cameraUtil.GetEnemiesWithinCameraRadius(config.CameraRadius);
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    drawList.AddLine(
                        screenCenter,
                        enemy.ScreenPos,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 0.5f)),
                        2.0f
                    );
                    drawList.AddCircleFilled(
                        enemy.ScreenPos,
                        5f,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 0.8f))
                    );
                    string distanceText = $"Details: {enemy.NameNKind + " " + enemy.WorldDistance:F1}";
                    drawList.AddText(enemy.ScreenPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), distanceText);
                }
            }
        }
    }
}