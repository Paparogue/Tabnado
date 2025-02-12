using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Tabnado.UI;
using Tabnado.Objects;
using ImGuiNET;
using static FFXIVClientStructs.ThisAssembly;

namespace Tabnado.Others
{
    public class Tabnado
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

        public Tabnado(IClientState clientState, IObjectTable objectTable, ITargetManager targetManager, IChatGui chatGui, PluginConfig config, CameraUtil c2e, IGameGui gameGui, IPluginLog pluginLog, KeyDetection keyDetector)
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
        }

        public void Draw()
        {
            if (c2e == null)
                return;

            if (config.ShowDebugRaycast || config.ShowDebugSelection)
                c2e.UpdateEnemyList();

            if (keyDetector.IsKeyPressed())
            {
                c2e.UpdateEnemyList();
                var enemyList = c2e.GetFullEnemyList();
                var mObject = c2e.GetClosestEnemyInCircle();
                targetManager.Target = mObject?.GameObject;
            }

            ShowDebugSelection();
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
                    string distanceText = $"Distance: {enemy.WorldDistance:F1}";
                    drawList.AddText(enemy.ScreenPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), distanceText);
                }
            }
        }
    }
}
