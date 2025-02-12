using System.Numerics;
using Tabnado.Others;
using Dalamud.Plugin;
using ImGuiNET;
using Tabnado.Util;

namespace Tabnado.UI
{
    public class TabnadoUI
    {
        private bool settingsVisible = false;
        private PluginConfig config;
        private Others.Tabnado targetingManager;
        private IDalamudPluginInterface pluginInterface;
        private KeyDetection keyDetector;

        public TabnadoUI(IDalamudPluginInterface pluginInterface, PluginConfig config, Others.Tabnado targetingManager, KeyDetection keyDetector)
        {
            this.pluginInterface = pluginInterface;
            this.config = config;
            this.targetingManager = targetingManager;
            this.keyDetector = keyDetector;
        }

        public void ToggleVisibility()
        {
            settingsVisible = !settingsVisible;
        }

        public void Draw()
        {
            if (!settingsVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Tabnado Target Settings", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                bool configChanged = false;

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                ImGui.TextWrapped("WARNING: If your selected key conflicts with any game keybind, please unbind it in the game's keybind settings to avoid conflicts!");
                ImGui.PopStyleColor();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.BeginCombo("Target Key", config.SelectedKey))
                {
                    foreach (var key in KeyConfig.VirtualKeys)
                    {
                        bool isSelected = (config.SelectedKey).Equals(key.Key);
                        if (ImGui.Selectable(key.Key, isSelected))
                        {
                            config.SelectedKey = key.Key;
                            keyDetector.SetCurrentKey(key.Value);
                            configChanged = true;
                        }
                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (config.SelectedKey == "Tab")
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.65f, 0.0f, 1.0f));
                    ImGui.TextWrapped("Note: If using Tab, make sure to unbind the default target key in your game settings!");
                    ImGui.PopStyleColor();
                }

                int maxTargetDistance = config.MaxTargetDistance;
                if (ImGui.SliderInt("Max Target Distance", ref maxTargetDistance, 1, 55))
                {
                    config.MaxTargetDistance = maxTargetDistance;
                    configChanged = true;
                }

                int cameraRadius = config.CameraRadius;
                if (ImGui.SliderInt("Camera Search Radius", ref cameraRadius, 1, 1000))
                {
                    config.CameraRadius = cameraRadius;
                    configChanged = true;
                }

                int collisionMultiplier = config.CollissionMultiplier;
                if (ImGui.SliderInt("Collision Multiplier", ref collisionMultiplier, 1, 16))
                {
                    config.CollissionMultiplier = collisionMultiplier;
                    configChanged = true;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.65f, 0.0f, 1.0f));
                ImGui.TextWrapped("Higher multiplier: better accuracy visbility check but higher CPU usage!");
                ImGui.PopStyleColor();

                bool onlyAttackable = config.OnlyHostilePlayers;
                if (ImGui.Checkbox("Target Only Hostile Players", ref onlyAttackable))
                {
                    config.OnlyHostilePlayers = onlyAttackable;
                    configChanged = true;
                }

                bool onlyBattleNPCs = config.OnlyBattleNPCs;
                if (ImGui.Checkbox("Target Only Battle NPCs", ref onlyBattleNPCs))
                {
                    config.OnlyBattleNPCs = onlyBattleNPCs;
                    configChanged = true;
                }

                bool onlyVisibleObjects = config.OnlyVisibleObjects;
                if (ImGui.Checkbox("Target Only Visible Objects", ref onlyVisibleObjects))
                {
                    config.OnlyVisibleObjects = onlyVisibleObjects;
                    configChanged = true;
                }

                ImGui.Separator();

                bool showDebugRaycast = config.ShowDebugRaycast;
                if (ImGui.Checkbox("Show Debug Raycast Info", ref showDebugRaycast))
                {
                    config.ShowDebugRaycast = showDebugRaycast;
                    configChanged = true;
                }

                bool showDebug = config.ShowDebugSelection;
                if (ImGui.Checkbox("Show Debug Selection Info", ref showDebug))
                {
                    config.ShowDebugSelection = showDebug;
                    configChanged = true;
                }

                if (configChanged)
                    config.Save();
            }
            ImGui.End();
        }
    }
}