using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;
using Tabnado.Util;

namespace Tabnado.UI
{
    public class TabnadoUI
    {
        private static readonly Vector4 WarningColor = new(1.0f, 0.0f, 0.0f, 1.0f);
        private static readonly Vector4 NoteColor = new(1.0f, 0.65f, 0.0f, 1.0f);

        private bool settingsVisible = false;
        private readonly PluginConfig config;
        private readonly Tabnado tabnado;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly KeyDetection keyDetection;

        public TabnadoUI(IDalamudPluginInterface pluginInterface, PluginConfig config, Tabnado targetingManager, KeyDetection keyDetector)
        {
            this.pluginInterface = pluginInterface;
            this.config = config;
            this.tabnado = targetingManager;
            this.keyDetection = keyDetector;
        }

        public void ToggleVisibility()
        {
            settingsVisible = !settingsVisible;
        }

        public void Draw()
        {
            if (!settingsVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(450, 450), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Tabnado Target Settings", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                bool configChanged = false;

                ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
                ImGui.TextWrapped("WARNING: If your selected key conflicts with any game keybind, please unbind it in the game's keybind settings to avoid conflicts!");
                ImGui.PopStyleColor();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.BeginCombo("Target Key", config.SelectedKey))
                {
                    foreach (var key in KeyConfig.VirtualKeys)
                    {
                        bool isSelected = config.SelectedKey.Equals(key.Key);
                        if (ImGui.Selectable(key.Key, isSelected))
                        {
                            config.SelectedKey = key.Key;
                            keyDetection.SetCurrentKey(key.Value);
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
                    ImGui.PushStyleColor(ImGuiCol.Text, NoteColor);
                    ImGui.TextWrapped("Note: If using Tab, make sure to unbind the default target key in your game settings!");
                    ImGui.PopStyleColor();
                }

                ImGui.Spacing();

                ImGui.TextDisabled("Targeting Range Settings");
                ImGui.Separator();

                int maxTargetDistance = config.MaxTargetDistance;
                if (ImGui.SliderInt("Max Target Distance (yalms)", ref maxTargetDistance, 1, 55))
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

                if(config.OnlyVisibleObjects) {

                    int visibilityPercent = config.VisibilityPercent;
                    if (ImGui.SliderInt("Visibility Check Percent", ref visibilityPercent, 1, 100))
                    {
                        config.VisibilityPercent = visibilityPercent;
                        configChanged = true;
                    }

                    int rayMultiplier = config.RaycastMultiplier;
                    if (ImGui.SliderInt("Raycast Multiplier", ref rayMultiplier, 1, 16))
                    {
                        config.RaycastMultiplier = rayMultiplier;
                        configChanged = true;
                    }

                    int rayPercent = config.RayCastPercent;
                    if (ImGui.SliderInt("Raycast Percent", ref rayPercent, 1, 100))
                    {
                        config.RayCastPercent = rayPercent;
                        configChanged = true;
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, NoteColor);
                    ImGui.TextWrapped("Higher multiplier values increase targeting accuracy but may reduce performance.");
                    ImGui.PopStyleColor();
                }

                ImGui.Spacing();

                ImGui.TextDisabled("Target Reset Options");
                ImGui.Separator();

                bool useCameraRotationReset = config.UseCameraRotationReset;
                if (ImGui.Checkbox("Camera rotation resets selection", ref useCameraRotationReset))
                {
                    config.UseCameraRotationReset = useCameraRotationReset;
                    configChanged = true;
                }

                if(useCameraRotationReset) {
                    int rotationPercent = config.RotationPercent;
                    if (ImGui.SliderInt("% camera movement until reset", ref rotationPercent, 1, 100))
                    {
                        config.RotationPercent = rotationPercent;
                        configChanged = true;
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
                    ImGui.TextWrapped("Target selection resets when camera rotation exceeding this threshold.");
                    ImGui.PopStyleColor();
                }

                bool useCombatantReset = config.UseCombatantReset;
                if (ImGui.Checkbox("Combatants resets selection", ref useCombatantReset))
                {
                    config.UseCombatantReset = useCombatantReset;
                    configChanged = true;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
                ImGui.TextWrapped("Target selection resets when new entity enters the specified camera search radius."); ;
                ImGui.PopStyleColor();

                bool useNewTargetReset = config.UseNewTargetReset;
                if (ImGui.Checkbox("Use new main target as reset", ref useNewTargetReset))
                {
                    config.UseNewTargetReset = useNewTargetReset;
                    configChanged = true;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
                ImGui.TextWrapped("Target selection resets when a new entity becomes the nearest valid target.");
                ImGui.PopStyleColor();

                ImGui.Spacing();

                ImGui.TextDisabled("Target Filtering Options");
                ImGui.Separator();

                bool onlyAttackable = config.OnlyHostilePlayers;
                if (ImGui.Checkbox("Target Only Hostile Players (PvP)", ref onlyAttackable))
                {
                    config.OnlyHostilePlayers = onlyAttackable;
                    configChanged = true;
                }

                bool onlyBattleNPCs = config.OnlyBattleNPCs;
                if (ImGui.Checkbox("Target Only Battle NPCs (PvE)", ref onlyBattleNPCs))
                {
                    config.OnlyBattleNPCs = onlyBattleNPCs;
                    configChanged = true;
                }

                bool onlyVisibleObjects = config.OnlyVisibleObjects;
                if (ImGui.Checkbox("Target Only Visible Objects (PvE/PvP)", ref onlyVisibleObjects))
                {
                    config.OnlyVisibleObjects = onlyVisibleObjects;
                    configChanged = true;
                }

                ImGui.Spacing();

                ImGui.TextDisabled("Other Options");
                ImGui.Separator();

                ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
                ImGui.TextWrapped("Warning: Enabling this option may significantly impact performance!");
                ImGui.PopStyleColor();

                bool clearTargetTable = config.ClearTargetTable;
                if (ImGui.Checkbox("Periodically Clear Dead Target Table (PvE/PvP)", ref clearTargetTable))
                {
                    config.ClearTargetTable = clearTargetTable;
                    configChanged = true;
                }

                if(clearTargetTable)
                {

                    int clearDeadTable = config.ClearDeadTable;
                    if (ImGui.SliderInt("Clear Dead Table every (ms)", ref clearDeadTable, 1, 2000))
                    {
                        config.ClearDeadTable = clearDeadTable;
                        configChanged = true;
                    }

                }

                /*
                bool drawSelection = config.DrawSelection;
                if (ImGui.Checkbox("Draw the Object that would be selected (PvE/PvP)", ref drawSelection))
                {
                    config.DrawSelection = drawSelection;
                    configChanged = true;
                }*/

                bool showDebugRaycast = config.ShowDebugRaycast;
                if (ImGui.Checkbox("Show Debug Raycast Info (PvE/PvP)", ref showDebugRaycast))
                {
                    config.ShowDebugRaycast = showDebugRaycast;
                    configChanged = true;
                }

                bool showDebug = config.ShowDebugSelection;
                if (ImGui.Checkbox("Show Debug Selection Info (PvE/PvP)", ref showDebug))
                {
                    config.ShowDebugSelection = showDebug;
                    configChanged = true;
                }

                int drawRefreshRate = config.DrawRefreshRate;
                if (ImGui.SliderInt("Draw Refresh Rate", ref drawRefreshRate, 1, 100))
                {
                    config.DrawRefreshRate = drawRefreshRate;
                    configChanged = true;
                }

                if (configChanged)
                    config.Save();
            }
            ImGui.End();
        }
    }
}