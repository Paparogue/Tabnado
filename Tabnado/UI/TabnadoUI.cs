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
                    ImGui.SameLine();
                    ImGui.TextDisabled("(?)");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Note: Using 'Tab' requires unbinding the default target key in your game settings to avoid conflicts.");
                    }
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

                if (config.OnlyVisibleObjects) {

                    int visibilityPercent = config.VisibilityPercent;
                    if (ImGui.SliderInt("Visibility Check Percent", ref visibilityPercent, 1, 100))
                    {
                        config.VisibilityPercent = visibilityPercent;
                        configChanged = true;
                    }

                    int rayPercent = config.RayCastPercent;
                    if (ImGui.SliderInt("Raycast Percent", ref rayPercent, 1, 100))
                    {
                        config.RayCastPercent = rayPercent;
                        configChanged = true;
                    }

                    int rayMultiplier = config.RaycastMultiplier;
                    if (ImGui.SliderInt("Raycast Multiplier", ref rayMultiplier, 1, 16))
                    {
                        config.RaycastMultiplier = rayMultiplier;
                        configChanged = true;
                    }
                }

                ImGui.Spacing();

                ImGui.TextDisabled("Target Reset Options");
                ImGui.Separator();

                bool useCameraRotationReset = config.UseCameraRotationReset;
                if (ImGui.Checkbox("Reset target on camera rotation", ref useCameraRotationReset))
                {
                    config.UseCameraRotationReset = useCameraRotationReset;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Automatically resets the target selection if the camera rotates beyond a certain threshold.");
                }

                if (useCameraRotationReset)
                {
                    int rotationPercent = config.RotationPercent;
                    if (ImGui.SliderInt("Rotation threshold (% movement)", ref rotationPercent, 1, 100))
                    {
                        config.RotationPercent = rotationPercent;
                        configChanged = true;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Set the percentage of camera rotation that will trigger a target reset.");
                    }
                }

                bool useCombatantReset = config.UseCombatantReset;
                if (ImGui.Checkbox("Reset target when a new combatant appears", ref useCombatantReset))
                {
                    config.UseCombatantReset = useCombatantReset;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Resets the target selection when a new combatant enters the camera's search area.");
                }

                bool useNewTargetReset = config.UseNewTargetReset;
                if (ImGui.Checkbox("Reset target on new nearest entity", ref useNewTargetReset))
                {
                    config.UseNewTargetReset = useNewTargetReset;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Resets the target selection when a closer valid target is detected.");
                }


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

                ImGui.TextDisabled("Debug Options");
                ImGui.Separator();

                bool clearTargetTable = config.ClearTargetTable;
                if (ImGui.Checkbox("Periodically Clear Dead Target Table (PvE/PvP)", ref clearTargetTable))
                {
                    config.ClearTargetTable = clearTargetTable;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                    ImGui.SetTooltip("WARNING: Enabling this option may significantly impact performance!");
                    ImGui.PopStyleColor();
                }

                if (clearTargetTable)
                {
                    int clearDeadTable = config.ClearDeadTable;
                    if (ImGui.SliderInt("Clear Dead Table every (ms)", ref clearDeadTable, 1, 2000))
                    {
                        config.ClearDeadTable = clearDeadTable;
                        configChanged = true;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                        ImGui.SetTooltip("WARNING: Enabling this option may significantly impact performance!");
                        ImGui.PopStyleColor();
                    }
                }

                bool showDebugRaycast = config.ShowDebugRaycast;
                if (ImGui.Checkbox("Show Debug Raycast Info (PvE/PvP)", ref showDebugRaycast))
                {
                    config.ShowDebugRaycast = showDebugRaycast;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                    ImGui.SetTooltip("WARNING: Enabling this option may significantly impact performance!");
                    ImGui.PopStyleColor();
                }

                bool showDebug = config.ShowDebugSelection;
                if (ImGui.Checkbox("Show Debug Selection Info (PvE/PvP)", ref showDebug))
                {
                    config.ShowDebugSelection = showDebug;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                    ImGui.SetTooltip("WARNING: Enabling this option may significantly impact performance!");
                    ImGui.PopStyleColor();
                }

                int drawRefreshRate = config.DrawRefreshRate;
                if (ImGui.SliderInt("Draw Refresh Rate", ref drawRefreshRate, 1, 100))
                {
                    config.DrawRefreshRate = drawRefreshRate;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                    ImGui.SetTooltip("WARNING: Enabling this option may significantly impact performance!");
                    ImGui.PopStyleColor();
                }

                float cameraDepth = config.CameraDepth;
                if (ImGui.SliderFloat("Camera Depth", ref cameraDepth, 1f, 10f, "%.1f"))
                {
                    config.CameraDepth = cameraDepth;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                    ImGui.SetTooltip("WARNING: Enabling this option may significantly impact performance!");
                    ImGui.PopStyleColor();
                }

                if (configChanged)
                    config.Save();
            }
            ImGui.End();
        }
    }
}