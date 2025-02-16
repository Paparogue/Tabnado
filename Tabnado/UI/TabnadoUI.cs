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
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Only targets within this distance (in yalms) are considered.");
                }

                int cameraRadius = config.CameraRadius;
                if (ImGui.SliderInt("Camera Search Radius", ref cameraRadius, 1, 1000))
                {
                    config.CameraRadius = cameraRadius;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Defines the radius around the middle of the camera in which objects are checked for targeting.");
                }

                if (config.OnlyVisibleObjects)
                {
                    int visibilityPercent = config.VisibilityPercent;
                    if (ImGui.SliderInt("Minimum Visibility (%)", ref visibilityPercent, 1, 100))
                    {
                        config.VisibilityPercent = visibilityPercent;
                        configChanged = true;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Specifies the minimum visibility percentage required for an object to be a valid target.");
                    }

                    int rayPercent = config.RayCastPercent;
                    if (ImGui.SliderInt("Raycast Transformation (%)", ref rayPercent, 1, 100))
                    {
                        config.RayCastPercent = rayPercent;
                        configChanged = true;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Adjusts the raycast points toward the center of the camera, refining the detection area.");
                    }

                    int rayMultiplier = config.RaycastMultiplier;
                    if (ImGui.SliderInt("Raycast Multiplier", ref rayMultiplier, 1, 16))
                    {
                        config.RaycastMultiplier = rayMultiplier;
                        configChanged = true;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Multiplies the base number of raycasts by 4.");
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
                if (ImGui.Checkbox("Target Only Hostile Players", ref onlyAttackable))
                {
                    config.OnlyHostilePlayers = onlyAttackable;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("When enabled, only hostile players will be targeted in PvP, excluding teammates and alliance members.");
                }

                bool onlyBattleNPCs = config.OnlyBattleNPCs;
                if (ImGui.Checkbox("Target Only Battle NPCs", ref onlyBattleNPCs))
                {
                    config.OnlyBattleNPCs = onlyBattleNPCs;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("When enabled, only Battle NPCs will be targeted, ignoring event NPCs, traders, and pets.");
                }

                bool onlyVisibleObjects = config.OnlyVisibleObjects;
                if (ImGui.Checkbox("Target Only Visible Objects", ref onlyVisibleObjects))
                {
                    config.OnlyVisibleObjects = onlyVisibleObjects;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("When enabled, only objects that meet the defined visibility threshold will be considered for targeting.");
                }

                ImGui.Spacing();

                ImGui.TextDisabled("Debug Options");
                ImGui.Separator();

                bool clearTargetTable = config.ClearTargetTable;
                if (ImGui.Checkbox("Reset Target Table", ref clearTargetTable))
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
                    if (ImGui.SliderInt("Reset Target Table every (ms)", ref clearDeadTable, 1, 2000))
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
                if (ImGui.Checkbox("Show Raycast Info", ref showDebugRaycast))
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
                if (ImGui.Checkbox("Show Selection Info", ref showDebug))
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