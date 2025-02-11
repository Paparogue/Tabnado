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

namespace Tabnado.Others
{
    public class Tabnado
    {
        private IClientState clientState;
        private IObjectTable objectTable;
        private ITargetManager targetManager;
        private IChatGui chatGui;
        private PluginConfig config;
        private Camera2Enemy c2e;
        private IGameGui gameGui;
        private IPluginLog pluginLog;
        private KeyDetector keyDetector;
        private bool wasTabPressed = false;

        public Tabnado(IClientState clientState, IObjectTable objectTable, ITargetManager targetManager, IChatGui chatGui, PluginConfig config, Camera2Enemy c2e, IGameGui gameGui, IPluginLog pluginLog)
        {
            this.clientState = clientState;
            this.objectTable = objectTable;
            this.targetManager = targetManager;
            this.chatGui = chatGui;
            this.config = config;
            this.c2e = c2e;
            this.gameGui = gameGui;
            this.pluginLog = pluginLog;
            this.keyDetector = new KeyDetector();
        }

        public void Draw()
        {
            if (c2e == null)
                return;

            if (keyDetector.IsTabPressed())
            {
                c2e.UpdateEnemyList();
                var enemyList = c2e.GetFullEnemyList();
                var mObject = c2e.GetClosestCameraEnemy();
                targetManager.Target = mObject?.GameObject;
            }

            ShowDebug();
        }


        private void ShowDebug()
        {
            if (config.ShowDebug == false) return;
            var screenCenter = new Vector2(ImGui.GetIO().DisplaySize.X / 2, ImGui.GetIO().DisplaySize.Y / 2);

            var drawList = ImGui.GetForegroundDrawList();
            drawList.AddCircleFilled(screenCenter, 3f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));

            c2e.UpdateEnemyList();
            var enemies = c2e.GetFullEnemyList();
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
                }
            }
        }
    }
}
