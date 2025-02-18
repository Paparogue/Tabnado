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
                string resetReason = "";

                bool[] triggers = new bool[3] { false, false, false };
                string[] triggerNames = new string[] { "Camera Rotation", "Combatant List", "New Target" };

                if (config.UseCameraRotationReset && cameraUtil.CameraExceedsRotation(config.RotationPercent[0], 0))
                {
                    triggers[0] = true;
                }

                if (!IsListEqual(lastEnemyList, enemies))
                {
                    triggers[1] = true;
                    lastEnemyList = new List<ScreenMonsterObject>(enemies);
                }

                if (enemies.Count > 0)
                {
                    var closestEnemy = enemies[0];
                    if (closestEnemy.GameObjectId != previousClosestTargetId)
                    {
                        triggers[2] = true;
                        previousClosestTargetId = closestEnemy.GameObjectId;
                    }
                }

                for (int i = 0; i < triggers.Length; i++)
                {
                    if (!triggers[i]) continue;

                    bool hasCombinations = false;
                    bool isEnabled = false;

                    switch (i)
                    {
                        case 0:
                            isEnabled = config.UseCameraRotationReset;
                            hasCombinations = config.ResetCombinations[i, 1] || config.ResetCombinations[i, 2];
                            break;
                        case 1:
                            isEnabled = config.UseCombatantReset;
                            hasCombinations = config.ResetCombinations[i, 0] || config.ResetCombinations[i, 2];
                            break;
                        case 2:
                            isEnabled = config.UseNewTargetReset;
                            hasCombinations = config.ResetCombinations[i, 0] || config.ResetCombinations[i, 1];
                            break;
                    }

                    if (!isEnabled) continue;

                    if (hasCombinations)
                    {
                        bool allCombinationsMet = true;
                        var activeCombinations = new List<string>();

                        for (int j = 0; j < triggers.Length; j++)
                        {
                            if (j == i) continue;

                            if (config.ResetCombinations[i, j])
                            {
                                if (j == 0)
                                {
                                    bool rotationValid = false;
                                    switch (i)
                                    {
                                        case 1:
                                            rotationValid = cameraUtil.CameraExceedsRotation(config.RotationPercent[1], 1);
                                            break;
                                        case 2:
                                            rotationValid = cameraUtil.CameraExceedsRotation(config.RotationPercent[2], 2);
                                            break;
                                    }
                                    if (!rotationValid)
                                    {
                                        allCombinationsMet = false;
                                    }
                                    else
                                    {
                                        activeCombinations.Add(triggerNames[j]);
                                    }
                                }
                                else
                                {
                                    if (!triggers[j])
                                    {
                                        allCombinationsMet = false;
                                    }
                                    else
                                    {
                                        activeCombinations.Add(triggerNames[j]);
                                    }
                                }
                            }
                        }

                        if (allCombinationsMet && activeCombinations.Count > 0)
                        {
                            resetTarget = true;
                            resetReason = $"Combination: {triggerNames[i]} + {string.Join(" + ", activeCombinations)}";
                            break;
                        }
                    }
                    else
                    {
                        resetTarget = true;
                        resetReason = $"Standalone: {triggerNames[i]}";
                        break;
                    }
                }

                if (config.ShowDebugSelection && resetTarget)
                {
                    pluginLog.Warning($"Reset triggered by: {resetReason}");
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
                float rotationLength = cameraUtil.GetRotationPercentage(0);
                float maxThreshold = ((float)config.RotationPercent[0] / 100f);

                drawList.AddCircle(
                    screenCenter,
                    rotationLength * ImGui.GetIO().DisplaySize.Y,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 1f)),
                    32
                );

                if (rotationLength >= maxThreshold)
                {
                    drawList.AddCircle(
                        screenCenter,
                        maxThreshold * ImGui.GetIO().DisplaySize.Y,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.65f, 0, 1f)),
                        32
                    );
                }
                else
                {
                    drawList.AddCircle(
                        screenCenter,
                        maxThreshold * ImGui.GetIO().DisplaySize.Y,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.5f)),
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
                    string distanceText = $"Details: {enemy.NameNKind} {enemy.WorldDistance:F1}";
                    drawList.AddText(enemy.ScreenPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), distanceText);
                }
            }
        }
    }
}