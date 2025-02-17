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
using static Tabnado.Util.CameraScene;
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
        private CameraScene cameraUtil;
        private IGameGui gameGui;
        private IPluginLog pluginLog;
        private KeyDetection keyDetection;
        private bool wasTabPressed = false;
        private int currentEnemyIndex;
        private List<ScreenMonsterObject> lastEnemyList;
        private DateTime lastClearTime;
        private ulong previousClosestTargetId;
        private const int CIRCLE_SEGMENTS = 16;
        private Vector3[] circlePoints;
        private bool circlePointsInitialized = false;

        public Tabnado(IClientState clientState, IObjectTable objectTable, ITargetManager targetManager,
                      IChatGui chatGui, PluginConfig config, CameraScene cameraUtil, IGameGui gameGui,
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

            List<ScreenMonsterObject> enemies = null!;
            var currentTime = DateTime.Now;
            bool clearTargetUpdate = config.ClearTargetTable &&
                                     (currentTime - lastClearTime).TotalMilliseconds > (double)config.ClearDeadTable;

            if (((config.ShowDebugRaycast || config.ShowDebugSelection) &&
                 (currentTime - lastClearTime).TotalMilliseconds > (double)config.DrawRefreshRate)
                || clearTargetUpdate)
            {
                cameraUtil.UpdateEnemyList();
                enemies = cameraUtil.GetEnemiesWithinCameraRadius(config.CameraRadius);
                lastClearTime = currentTime;

                if (enemies.Count <= 0)
                {
                    currentEnemyIndex = -1;
                    lastEnemyList.Clear();
                    previousClosestTargetId = 0;
                }
            }

            if (keyDetection.IsKeyPressed())
            {
                cameraUtil.UpdateEnemyList();
                enemies = cameraUtil.GetEnemiesWithinCameraRadius(config.CameraRadius);
                bool resetTarget = false;
                bool[] triggers = new bool[3] { false, false, false };

                if (config.UseCameraRotationReset && cameraUtil.CameraExceedsRotation())
                {
                    if (config.ShowDebugSelection)
                        pluginLog.Warning("CameraExceedsRotationReset triggered.");
                    triggers[0] = true;
                }

                if (config.UseCombatantReset && !IsListEqual(lastEnemyList, enemies))
                {
                    if (config.ShowDebugSelection)
                        pluginLog.Warning("UseCombatantReset triggered.");
                    triggers[1] = true;
                    lastEnemyList = new List<ScreenMonsterObject>(enemies);
                }

                if (config.UseNewTargetReset && enemies.Count > 0)
                {
                    var closestEnemy = enemies[0];
                    if (closestEnemy.GameObjectId != previousClosestTargetId)
                    {
                        if (config.ShowDebugSelection)
                            pluginLog.Warning("UseNewTargetReset triggered.");
                        triggers[2] = true;
                        previousClosestTargetId = closestEnemy.GameObjectId;
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    if (!triggers[i]) continue;

                    bool hasCombinations = false;
                    for (int j = 0; j < 3; j++)
                    {
                        if (j != i && config.ResetCombinations[i, j])
                        {
                            hasCombinations = true;
                            break;
                        }
                    }

                    if (!hasCombinations)
                    {
                        if (config.ShowDebugSelection)
                            pluginLog.Warning($"Condition {i} triggered reset (no combinations)");
                        resetTarget = true;
                        break;
                    }
                    else
                    {
                        bool allCombinationsMet = true;
                        for (int j = 0; j < 3; j++)
                        {
                            if (j != i && config.ResetCombinations[i, j] && !triggers[j])
                            {
                                allCombinationsMet = false;
                                break;
                            }
                        }

                        if (allCombinationsMet)
                        {
                            if (config.ShowDebugSelection)
                                pluginLog.Warning($"Condition {i} triggered reset (all combinations met)");
                            resetTarget = true;
                            break;
                        }
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

            var ids1 = new HashSet<ulong>(list1.Select(x => x.GameObjectId));
            var ids2 = new HashSet<ulong>(list2.Select(x => x.GameObjectId));

            return ids1.SetEquals(ids2);
        }

        private void ShowDebugSelection()
        {
            if (!config.ShowDebugSelection) return;

            var screenCenter = new Vector2(ImGui.GetIO().DisplaySize.X / 2, ImGui.GetIO().DisplaySize.Y / 2);
            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddCircleFilled(screenCenter, 3f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));

            if (config.UseCameraRotationReset)
            {
                float rotationLength = cameraUtil.getRotationLength();
                float maxThreshold = ((float)config.RotationPercent / 100f);

                drawList.AddCircle(
                    screenCenter,
                    rotationLength * ImGui.GetIO().DisplaySize.Y,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1f)),
                    32
                );

                drawList.AddCircle(
                    screenCenter,
                    maxThreshold * ImGui.GetIO().DisplaySize.Y,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.5f)),
                    32
                );
            }

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