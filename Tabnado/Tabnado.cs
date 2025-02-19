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
        private CameraScene cameraScene;
        private IGameGui gameGui;
        private IPluginLog log;
        private KeyDetection keyDetection;
        private bool wasTabPressed;
        private int currentEnemyIndex;
        private List<ScreenMonsterObject> lastEnemyList;
        private DateTime lastClearTime;
        private ulong previousClosestTargetId;
        private Vector3[] circlePoints;
        private const int CIRCLE_SEGMENTS = 16;

        public Tabnado(Plugin plugin)
        {
            this.clientState = plugin.ClientState;
            this.objectTable = plugin.ObjectTable;
            this.targetManager = plugin.TargetManager;
            this.chatGui = plugin.ChatGUI;
            this.config = plugin.PluginConfig;
            this.cameraScene = plugin.CameraScene;
            this.gameGui = plugin.GameGUI;
            this.log = plugin.Log;
            this.keyDetection = plugin.KeyDetection;
            lastClearTime = DateTime.Now;
            currentEnemyIndex = -1;
            wasTabPressed = false;
            lastEnemyList = new();
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
        }

        public void Draw()
        {
            if (cameraScene == null)
                return;

            List<ScreenMonsterObject> enemies = null!;
            var buttonPressed = keyDetection.IsKeyPressed();
            var currentTime = DateTime.Now;
            bool clearTargetUpdate = config.ClearTargetTable &&
                                     (currentTime - lastClearTime).TotalMilliseconds > config.ClearDeadTable;

            if (((config.ShowDebugRaycast || config.ShowDebugSelection && !buttonPressed) &&
                 (currentTime - lastClearTime).TotalMilliseconds > config.DrawRefreshRate)
                || clearTargetUpdate)
            {
                cameraScene.UpdateSceneList();
                enemies = cameraScene.GetObjectInsideRadius(config.CameraRadius);
                lastClearTime = currentTime;

                if (enemies.Count <= 0)
                {
                    currentEnemyIndex = -1;
                    lastEnemyList.Clear();
                    previousClosestTargetId = 0;
                }
            }

            if (buttonPressed)
            {
                cameraScene.UpdateSceneList();
                enemies = cameraScene.GetObjectInsideRadius(config.CameraRadius);
                string[] triggerNames = new string[] { "Camera Rotation", "Combatant List", "New Target" };
                bool resetTarget = false;
                string resetReason = "";
                bool[] triggers = new bool[3]
                {
                    cameraScene.CameraExceedsRotation(config.RotationPercent[0], 0), //Trigger Base (Camera Rotation)
                    !IsListEqual(lastEnemyList, enemies), //Trigger Base (Combatant List)
                    enemies.Count > 0 && (enemies[0].GameObjectId != previousClosestTargetId), //Trigger Base (New Targeting)
                };

                bool[,] configCheck = new bool[3, 3]
                {
                    {
                        config.UseCameraRotationReset, //BASE COMBO (Camera Rotation)
                        config.ResetCombinations[0, 0], // Sub Combo B (Combatant List)
                        config.ResetCombinations[0, 1] // Sub Combo C (New Targeting)
                    },
                    {
                        config.UseCombatantReset, //BASE COMBO (Combatant List)
                        config.ResetCombinations[1, 0], // Sub Combo (Camera Rotation)
                        config.ResetCombinations[1, 1] // Sub Combo (New Targeting)
                    },
                    {
                        config.UseNewTargetReset, //BASE COMBO (New Targeting)
                        config.ResetCombinations[2, 0], // Sub Combo (Camera Rotation)
                        config.ResetCombinations[2, 1] // Sub Combo (Combatant List)
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
                            int triggerIndex = (subIndex == 1) ? (baseIndex + 1) % 3 : (baseIndex + 2) % 3;
                            if (triggers[triggerIndex])
                            {
                                activeSubCombos.Add(triggerNames[triggerIndex]);
                            }
                            subComboMet &= triggers[triggerIndex];
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
                        break;
                    }
                }

                if (config.ShowDebugSelection && resetTarget)
                {
                    log.Warning($"Reset triggered by: {resetReason}");
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

                if (enemies != null && enemies.Count > 0)
                {
                    lastEnemyList = new List<ScreenMonsterObject>(enemies);
                    previousClosestTargetId = enemies[0].GameObjectId;
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
            var drawList = ImGui.GetBackgroundDrawList();

            drawList.AddCircleFilled(screenCenter, 3f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));

            if (config.UseCameraRotationReset)
            {
                float rotationLength = cameraScene.GetRotationPercentage(0);
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

            var enemies = cameraScene.GetObjectInsideRadius(config.CameraRadius);
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