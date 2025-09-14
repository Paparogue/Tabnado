using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Tabnado.UI;
using Dalamud.Bindings.ImGui;
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
        private int currentEnemyIndex;
        private List<ScreenObject> lastEnemyList;
        private DateTime lastClearTime;
        private ulong previousClosestTargetId;
        private Vector3[] circlePoints;
        public bool[] cameraFlag;
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

        public void TargetFunc()
        {
            if (cameraScene is null)
            {
                log.Error("Camera Scene is null. This should not happen!");
                return;
            }

            List<ScreenObject> enemies = null!;

            try
            {
                cameraScene.UpdateSceneList();
                enemies = cameraScene.GetObjectsInSelectionArea(config.AlternativeTargeting);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to update scene list: {ex}");
                return;
            }

            string[] triggerNames = new string[] { "Camera Rotation", "New Target", "New Closest Target" };
            bool resetTarget = false;
            string resetReason = "";

            bool[] triggers = new bool[3]
            {
                    cameraFlag[0],
                    !IsListEqual(lastEnemyList, enemies),
                    enemies.Count > 0 && enemies[0].GameObjectId != previousClosestTargetId,
            };

            bool[,] configCheck = new bool[3, 3]
            {
                    {
                        config.BaseCameraReset,
                        config.ResetCombinations[0, 0],
                        config.ResetCombinations[0, 1]
                    },
                    {
                        config.BaseCombatantReset,
                        config.ResetCombinations[1, 1],
                        config.ResetCombinations[1, 0]
                    },
                    {
                        config.BaseNewTargetReset,
                        config.ResetCombinations[2, 0],
                        config.ResetCombinations[2, 1]
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
                    if (enemies.Count > 0)
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

        private bool IsListEqual(List<ScreenObject> list1, List<ScreenObject> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            var ids1 = new HashSet<ulong>(list1.Select(x => x.GameObjectId));
            var ids2 = new HashSet<ulong>(list2.Select(x => x.GameObjectId));

            return ids1.SetEquals(ids2);
        }

        public void ShowDebugSelection()
        {
            if (!config.ShowDebugSelection) return;

            Vector2 screenCenter = cameraScene.GetPositionFromMonitor();
            var drawList = ImGui.GetBackgroundDrawList();

            drawList.AddCircleFilled(screenCenter, 3f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));

            if (config.BaseCameraReset)
            {
                float rotationLength = cameraScene.GetRotationPercentage(0);
                float maxThreshold = config.RotationPercent[0] / 100f;

                if (rotationLength < maxThreshold && !cameraFlag[0])
                {
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

            if (config.UseRectangleSelection)
            {
                float screenWidth = ImGui.GetIO().DisplaySize.X;
                float screenHeight = ImGui.GetIO().DisplaySize.Y;

                float leftExtent = screenWidth * (config.RectangleLeft / 100f);
                float rightExtent = screenWidth * (config.RectangleRight / 100f);
                float topExtent = screenHeight * (config.RectangleTop / 100f);
                float bottomExtent = screenHeight * (config.RectangleBottom / 100f);

                float leftEdge = Math.Max(0, screenCenter.X - leftExtent);
                float rightEdge = Math.Min(screenWidth, screenCenter.X + rightExtent);
                float topEdge = Math.Max(0, screenCenter.Y - topExtent);
                float bottomEdge = Math.Min(screenHeight, screenCenter.Y + bottomExtent);

                Vector2 topLeft = new Vector2(leftEdge, topEdge);
                Vector2 bottomRight = new Vector2(rightEdge, bottomEdge);

                drawList.AddRect(
                    topLeft,
                    bottomRight,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(2, 0, 0, 1f)),
                    0.0f,
                    ImDrawFlags.None,
                    2.0f
                );
            }
            else
            {
                drawList.AddCircle(
                    screenCenter,
                    config.CameraRadius,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(2, 0, 0, 1f)),
                    32
                );
            }

            var enemies = cameraScene.GetObjectsInSelectionArea(config.AlternativeTargeting);
            if (enemies != null && enemies.Count > 0)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    bool isClosest = i == 0;

                    Vector4 lineColor = isClosest
                        ? new Vector4(0, 1, 0.5f, 0.8f)
                        : new Vector4(1, 1, 0, 0.5f);

                    float lineThickness = isClosest ? 3.0f : 2.0f;

                    drawList.AddLine(
                        screenCenter,
                        enemy.ScreenPos,
                        ImGui.ColorConvertFloat4ToU32(lineColor),
                        lineThickness
                    );

                    Vector4 circleColor = isClosest
                        ? new Vector4(0, 1, 0.5f, 1f)
                        : new Vector4(1, 1, 0, 0.8f);

                    float circleSize = isClosest ? 7f : 5f;

                    drawList.AddCircleFilled(
                        enemy.ScreenPos,
                        circleSize,
                        ImGui.ColorConvertFloat4ToU32(circleColor)
                    );

                    if (isClosest)
                    {
                        drawList.AddCircle(
                            enemy.ScreenPos,
                            circleSize + 2f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.8f)),
                            16,
                            2.0f
                        );
                    }

                    Vector2 infoBoxStart = new Vector2(enemy.ScreenPos.X + 15, enemy.ScreenPos.Y - 10);
                    Vector4 bgColor = isClosest
                        ? new Vector4(0, 0.15f, 0.1f, 0.9f)
                        : new Vector4(0.1f, 0.1f, 0.1f, 0.85f);

                    List<string> infoLines = new List<string>();
                    infoLines.Add($"[{i + 1}] {enemy.Name}");
                    infoLines.Add($"Type: {enemy.ObjectType}");
                    infoLines.Add($"Distance: {enemy.WorldDistance:F1}y");
                    infoLines.Add($"Camera: {enemy.CameraDistance:F0}px");

                    if (enemy.IsHostile)
                        infoLines.Add("Hostile");
                    else if (enemy.IsNeutral)
                        infoLines.Add("Neutral");
                    else if (enemy.IsPlayer)
                        infoLines.Add("Player");

                    if (enemy.IsPet)
                        infoLines.Add($"Pet (Btn: {enemy.Battalion})");

                    float maxWidth = 0;
                    foreach (var line in infoLines)
                    {
                        var textSize = ImGui.CalcTextSize(line);
                        if (textSize.X > maxWidth)
                            maxWidth = textSize.X;
                    }

                    float boxHeight = infoLines.Count * 16 + 8;
                    float boxWidth = maxWidth + 16;

                    drawList.AddRectFilled(
                        infoBoxStart,
                        new Vector2(infoBoxStart.X + boxWidth, infoBoxStart.Y + boxHeight),
                        ImGui.ColorConvertFloat4ToU32(bgColor),
                        5.0f
                    );

                    if (isClosest)
                    {
                        drawList.AddRect(
                            infoBoxStart,
                            new Vector2(infoBoxStart.X + boxWidth, infoBoxStart.Y + boxHeight),
                            ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0.5f, 0.8f)),
                            5.0f,
                            ImDrawFlags.None,
                            2.0f
                        );
                    }

                    float yOffset = 4;
                    foreach (var line in infoLines)
                    {
                        Vector4 textColor = new Vector4(1, 1, 1, 1);

                        if (line.Contains("Hostile"))
                            textColor = new Vector4(1, 0.3f, 0.3f, 1);
                        else if (line.Contains("Neutral"))
                            textColor = new Vector4(1, 1, 0.5f, 1);
                        else if (line.Contains("Player"))
                            textColor = new Vector4(0.5f, 0.8f, 1, 1);
                        else if (line.Contains("Pet"))
                            textColor = new Vector4(0.8f, 0.5f, 1, 1);
                        else if (i == 0 && line.StartsWith("["))
                            textColor = new Vector4(0.5f, 1, 0.7f, 1);

                        drawList.AddText(
                            new Vector2(infoBoxStart.X + 8, infoBoxStart.Y + yOffset),
                            ImGui.ColorConvertFloat4ToU32(textColor),
                            line
                        );
                        yOffset += 16;
                    }
                }
            }
        }
    }
}