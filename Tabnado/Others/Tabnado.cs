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

namespace Tabnado.Others
{
    public unsafe class Tabnado
    {
        private IClientState clientState;
        private IObjectTable objectTable;
        private ITargetManager targetManager;
        private IChatGui chatGui;
        private PluginConfig config;
        private CameraUtil c2e;
        private IGameGui gameGui;
        private IPluginLog pluginLog;
        private KeyDetection keyDetector;
        private bool wasTabPressed = false;
        private int currentEnemyIndex;
        private List<ScreenMonsterObject> lastEnemyList;
        private DateTime lastUpdateTime = DateTime.Now;
        private const int CIRCLE_SEGMENTS = 16;
        private Vector3[] circlePoints;
        private bool circlePointsInitialized = false;

        public Tabnado(IClientState clientState, IObjectTable objectTable, ITargetManager targetManager,
                      IChatGui chatGui, PluginConfig config, CameraUtil c2e, IGameGui gameGui,
                      IPluginLog pluginLog, KeyDetection keyDetector)
        {
            this.clientState = clientState;
            this.objectTable = objectTable;
            this.targetManager = targetManager;
            this.chatGui = chatGui;
            this.config = config;
            this.c2e = c2e;
            this.gameGui = gameGui;
            this.pluginLog = pluginLog;
            this.keyDetector = keyDetector;
            this.currentEnemyIndex = -1;
            this.lastEnemyList = new();
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
                    characterPos.X + (circlePoints[index].X * radius),
                    characterPos.Y + 0.1f,
                    characterPos.Z + (circlePoints[index].Z * radius)
                );

                Vector2 screenPos;
                bool inView;
                gameGui.WorldToScreen(worldPoint, out screenPos, out inView);

                if (i > 0 && (inView || lastInView))
                {
                    float time = (float)(DateTime.Now.Millisecond) / 1000f;
                    float alpha = 0.4f + (MathF.Sin(time * MathF.PI * 2) * 0.2f);

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
            if (c2e == null)
                return;

            if (keyDetector.IsKeyPressed())
            {
                c2e.UpdateEnemyList();
                var enemies = c2e.GetEnemiesWithinCameraRadius(config.CameraRadius);

                if (!IsListEqual(lastEnemyList, enemies))
                {
                    currentEnemyIndex = -1;
                    lastEnemyList = new List<ScreenMonsterObject>(enemies);
                }

                if (enemies.Count > 0)
                {
                    currentEnemyIndex++;
                    if (currentEnemyIndex >= enemies.Count)
                        currentEnemyIndex = 0;

                    targetManager.Target = enemies[currentEnemyIndex].GameObject;
                }
                else
                {
                    currentEnemyIndex = -1;
                    lastEnemyList.Clear();
                }
            }

            if (config.ShowDebugRaycast || config.ShowDebugSelection || config.DrawSelection)
            {
                var currentTime = DateTime.Now;
                if (((currentTime - lastUpdateTime).TotalMilliseconds >= config.DrawRefreshRate) || (config.ShowDebugRaycast || config.ShowDebugSelection))
                {
                    c2e.UpdateEnemyList();
                    lastUpdateTime = currentTime;
                }
            }

            if (config.DrawSelection)
            {
                var enemies = c2e.GetEnemiesWithinCameraRadius(config.CameraRadius);
                if (enemies != null && enemies.Count > 0)
                {
                    int nextIndex = (currentEnemyIndex + 1) >= enemies.Count ? 0 : currentEnemyIndex + 1;

                    if (nextIndex < enemies.Count)
                    {
                        var nextTarget = enemies[nextIndex];
                        if (nextTarget?.GameObject is IGameObject gameObject)
                        {
                            Draw3DSelectionCircle((Character*)gameObject.Address);
                        }
                    }
                }
            }

            ShowDebugSelection();
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

            var enemies = c2e.GetEnemiesWithinCameraRadius(config.CameraRadius);
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