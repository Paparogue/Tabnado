﻿using System;
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
using static Tabnado.Util.CameraScene;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Tabnado.Util
{
    public unsafe class TargetingController
    {
        private readonly IClientState clientState;
        private readonly IObjectTable objectTable;
        private readonly ITargetManager targetManager;
        private readonly IChatGui chatGui;
        private readonly PluginConfig config;
        private readonly CameraScene cameraScene;
        private readonly IGameGui gameGui;
        private readonly IPluginLog log;
        private readonly KeyDetection keyDetection;
        private int currentEnemyIndex;
        private List<ScreenObject> lastEnemyList;
        private DateTime lastClearTime;
        private ulong previousClosestTargetId;
        private Vector3[] circlePoints;
        private bool[] cameraFlag;
        private const int CIRCLE_SEGMENTS = 16;

        public TargetingController(Plugin plugin)
        {
            clientState = plugin.ClientState;
            objectTable = plugin.ObjectTable;
            targetManager = plugin.TargetManager;
            chatGui = plugin.ChatGUI;
            config = plugin.PluginConfig;
            cameraScene = plugin.CameraScene;
            gameGui = plugin.GameGUI;
            log = plugin.Log;
            keyDetection = plugin.KeyDetection;
            lastClearTime = DateTime.Now;
            circlePoints = new Vector3[CIRCLE_SEGMENTS];
            currentEnemyIndex = 0;
            previousClosestTargetId = 0;
            lastEnemyList = new();
            cameraFlag = new bool[] { false, false, false };
            InitCirclePoints();
        }

        private void InitCirclePoints()
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
        }

        /* Maybe has some usage in future
        private void Draw3DSelectionCircle(Character* character)
        {
            var cameraManager = CameraManager.Instance();
            if (cameraManager == null || cameraManager->CurrentCamera == null) return;

            float radius = character->Height * 0.5f;
            Vector3 characterPos = new(character->Position.X, character->Position.Y, character->Position.Z);

            var drawList = ImGui.GetBackgroundDrawList();
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
        }*/

        public void Draw()
        {
            if (cameraScene is null)
            {
                log.Error("Camera Scene is null. This should not happen and we are sorry about that!");
                return;
            }
            if (cameraScene.GetCamera() is null || cameraScene.GetGroupManager() is null)
                cameraScene.InitManagerInstances();

            List<ScreenObject> enemies = null!;
            var buttonPressed = keyDetection.IsKeyPressed();
            var currentTime = DateTime.Now;
            bool clearTargetUpdate = config.ClearTargetTable &&
                                     (currentTime - lastClearTime).TotalMilliseconds > config.ClearTargetTableTimer;

            if ((config.ShowDebugRaycast || config.ShowDebugSelection && !buttonPressed) &&
                 (currentTime - lastClearTime).TotalMilliseconds > config.DrawRefreshRate
                || clearTargetUpdate)
            {
                cameraScene.UpdateSceneList();
                enemies = cameraScene.GetObjectInsideRadius(config.CameraRadius, config.AlternativeTargeting);
                lastClearTime = currentTime;

                if (enemies.Count <= 0 && config.ClearTargetTable)
                {
                    currentEnemyIndex = 0;
                    lastEnemyList.Clear();
                    previousClosestTargetId = 0;

                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (cameraScene.CameraExceedsRotation(config.RotationPercent[i], i, false))
                    cameraFlag[i] = true;
            }

            if (buttonPressed)
            {
                cameraScene.UpdateSceneList();
                enemies = cameraScene.GetObjectInsideRadius(config.CameraRadius, config.AlternativeTargeting);
                string[] triggerNames = new string[] { "Camera Rotation", "New Target", "New Closest Target" };
                bool resetTarget = false;
                string resetReason = "";

                //dont touch if not broken i guess
                bool[] triggers = new bool[3]
                {
                    cameraFlag[0], //BASE COMBO (Camera Rotation) 0
                    !IsListEqual(lastEnemyList, enemies), //BASE COMBO (New Target) 1 
                    enemies.Count > 0 && enemies[0].GameObjectId != previousClosestTargetId, //BASE COMBO (New Closest Target) 2
                };

                bool[,] configCheck = new bool[3, 3]
                {
                    {
                        config.BaseCameraReset, //BASE COMBO (Camera Rotation) 0
                        config.ResetCombinations[0, 0], // Sub Combo B (New Target) 1
                        config.ResetCombinations[0, 1] // Sub Combo C (New Closest Target) 2
                    },
                    {
                        config.BaseCombatantReset, //BASE COMBO (New Target) 1
                        config.ResetCombinations[1, 1], // Sub Combo (New Closest Target) 2
                        config.ResetCombinations[1, 0] // Sub Combo (Camera Rotation) 0
                    },
                    {
                        config.BaseNewTargetReset, //BASE COMBO (New Closest Target) 2
                        config.ResetCombinations[2, 0], // Sub Combo (Camera Rotation) 0
                        config.ResetCombinations[2, 1] // Sub Combo (New Target) 1
                    }
                };
                for (int baseIndex = 0; baseIndex < 3; baseIndex++)
                {
                    if (!configCheck[baseIndex, 0]) continue;

                    if (!triggers[baseIndex]) continue;

                    bool subComboRequired = false;
                    bool subComboMet = true;
                    List<string> activeSubCombos = new List<string>();

                    for (int subIndex = 1; subIndex < 3; subIndex++)
                    {
                        if (configCheck[baseIndex, subIndex])
                        {
                            subComboRequired = true;
                            int triggerIndex = subIndex == 1 ? (baseIndex + 1) % 3 : (baseIndex + 2) % 3;

                            bool triggerValue;
                            if (triggerIndex == 0)
                            {
                                triggerValue = cameraFlag[baseIndex];
                            }
                            else
                            {
                                triggerValue = triggers[triggerIndex];
                            }

                            if (triggerValue)
                            {
                                activeSubCombos.Add(triggerNames[triggerIndex]);
                            }
                            subComboMet &= triggerValue;
                        }
                    }

                    if (!subComboRequired || subComboMet)
                    {
                        resetTarget = true;
                        resetReason = $"Base: {triggerNames[baseIndex]}";
                        if (subComboRequired)
                        {
                            resetReason += $" with subcombos: {string.Join(" + ", activeSubCombos)}";
                        }
                        if(enemies.Count > 0)
                        {
                            cameraFlag[baseIndex] = false;
                            cameraScene.UpdateMatrice(baseIndex);
                        }
                        break;
                    }
                }

                if (config.ShowDebugSelection && resetTarget)
                {
                    log.Warning($"Reset triggered by: {resetReason}");
                    log.Warning("===================================");
                }

                if (enemies.Count > 0)
                {
                    var noTarget = config.ResetOnNoTarget && clientState.LocalPlayer?.TargetObjectId == 0;
                    if (noTarget || (resetTarget && (config.StickyTargetOnReset || (!config.StickyTargetOnReset && 
                        enemies[0].GameObject?.Address != targetManager.Target?.Address))))
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

                if (enemies != null && enemies.Count > 0)
                {
                    lastEnemyList = new List<ScreenObject>(enemies);
                    previousClosestTargetId = enemies[0].GameObjectId;
                }
            }

            if (config.ShowDebugSelection)
            {
                ShowDebugSelection();
            }
        }

        private bool IsListEqual(List<ScreenObject> list1, List<ScreenObject> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            var ids1 = new HashSet<ulong>(list1.Select(x => x.GameObjectId));
            var ids2 = new HashSet<ulong>(list2.Select(x => x.GameObjectId));

            return ids1.SetEquals(ids2);
        }

        private void ShowDebugSelection()
        {
            if (!config.ShowDebugSelection) return;

            Vector2 screenCenter = cameraScene.GetPositionFromMonitor();
            var drawList = ImGui.GetBackgroundDrawList();

            drawList.AddCircleFilled(screenCenter, 3f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));

            if (config.BaseCameraReset)
            {
                float rotationLength = cameraScene.GetRotationPercentage(0);
                float maxThreshold = config.RotationPercent[0] / 100f;

                if (rotationLength < maxThreshold && !cameraFlag[0]) {

                    drawList.AddCircle(
                        screenCenter,
                        rotationLength * ImGui.GetIO().DisplaySize.Y,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 1f)),
                        32
                    );

                }

                if (rotationLength >= maxThreshold || cameraFlag[0])
                {
                    drawList.AddCircle(
                        screenCenter,
                        maxThreshold * ImGui.GetIO().DisplaySize.Y,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.65f, 0, 1f)),
                        32
                    );
                }
            }

            drawList.AddCircle(
                screenCenter,
                config.CameraRadius,
                ImGui.ColorConvertFloat4ToU32(new Vector4(2, 0, 0, 1f)),
                32
            );

            var enemies = cameraScene.GetObjectInsideRadius(config.CameraRadius, config.AlternativeTargeting);
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
                    string distanceText = $"Details: {enemy.GlobalInfo} {enemy.WorldDistance:F1}";
                    drawList.AddText(enemy.ScreenPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), distanceText);
                }
            }
        }
    }
}