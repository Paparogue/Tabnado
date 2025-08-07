using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Bindings.ImGui;
using Tabnado.Util;

namespace Tabnado.UI
{
    public class TabnadoUI
    {
        private static readonly Vector4 WarningColor = new(1.0f, 0.0f, 0.0f, 1.0f);
        private static readonly Vector4 NoteColor = new(1.0f, 0.65f, 0.0f, 1.0f);

        private bool settingsVisible = false;
        private readonly PluginConfig config;
        private readonly IDalamudPluginInterface pluginInterface;

        public TabnadoUI(Plugin plugin)
        {
            this.pluginInterface = plugin.PluginInterface;
            this.config = plugin.PluginConfig;
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

                ImGui.TextDisabled("Targeting Settings");
                ImGui.Separator();

                float monitorX = config.MonitorX;
                if (ImGui.SliderFloat("Target Center X (%)", ref monitorX, 0, 100, "%.1f"))
                {
                    config.MonitorX = monitorX;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Adjusts horizontal position of targeting center. lower shifts left, higher shifts right.");
                }

                float monitorY = config.MonitorY;
                if (ImGui.SliderFloat("Target Center Y (%)", ref monitorY, 0, 100, "%.1f"))
                {
                    config.MonitorY = monitorY;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Adjusts vertical position of targeting center. lower shifts up, higher shifts down.");
                }

                int maxTargetDistance = config.MaxTargetDistance;
                if (ImGui.SliderInt("Max Target Distance (yalms)", ref maxTargetDistance, 1, 55))
                {
                    config.MaxTargetDistance = maxTargetDistance;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Sets the maximum targeting range. Objects beyond this distance (in yalms) will be ignored.");
                }

                int cameraRadius = config.CameraRadius;
                if (ImGui.SliderInt("Camera Search Radius", ref cameraRadius, 1, 1000))
                {
                    config.CameraRadius = cameraRadius;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Defines the circular search area around your target center for detecting targetable objects.");
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
                        ImGui.SetTooltip("Increases raycast density for target detection. Higher values improve accuracy but may impact performance.");
                    }
                }

                bool useCameraLerp = config.UseCameraLerp;
                if (ImGui.Checkbox("Camera Lerp", ref useCameraLerp))
                {
                    config.UseCameraLerp = useCameraLerp;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Automatically adjusts target center based on camera zoom. When zoomed out, targeting shifts upward to maintain accuracy.");
                }

                if (useCameraLerp)
                {
                    float cameraLerp = config.CameraLerp;
                    if (ImGui.SliderFloat("Camera Zoom Lerp", ref cameraLerp, 0.001f, 0.1f, "%.3f"))
                    {
                        config.CameraLerp = cameraLerp;
                        configChanged = true;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Controls how quickly the target center adjusts to camera zoom changes. Higher values mean faster adjustment.");
                    }
                }

                bool useDistanceLerp = config.UseDistanceLerp;
                if (ImGui.Checkbox("Distance Lerp", ref useDistanceLerp))
                {
                    config.UseDistanceLerp = useDistanceLerp;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Automatically raises the objects target point when getting closer, making nearby targets easier to select.");
                }

                if (useDistanceLerp)
                {
                    float distanceLerp = config.DistanceLerp;
                    if (ImGui.SliderFloat("Distance Lerp", ref distanceLerp, 0.01f, 10f, "%.2f"))
                    {
                        config.DistanceLerp = distanceLerp;
                        configChanged = true;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Controls how quickly the target point adjusts based on object distance. Higher values mean faster adjustment.");
                    }
                }

                bool alternativeTargeting = config.AlternativeTargeting;
                if (ImGui.Checkbox("Alternative Targeting Mode", ref alternativeTargeting))
                {
                    config.AlternativeTargeting = alternativeTargeting;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("In alternative mode, the closest target to the camera's center point is selected first. Afterward, you can cycle through targets based on proximity to your current selection.");
                }

                ImGui.Spacing();

                ImGui.TextDisabled("Target Reset Options");
                ImGui.Separator();

                bool resetOnNoTarget = config.ResetOnNoTarget;
                if (ImGui.Checkbox("No Target Reset", ref resetOnNoTarget))
                {
                    config.ResetOnNoTarget = resetOnNoTarget;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Resets to nearest target when no target is selected.");
                }

                bool useCameraRotationReset = config.BaseCameraReset;
                if (ImGui.Checkbox("Camera Rotation Reset##1", ref useCameraRotationReset))
                {
                    config.BaseCameraReset = useCameraRotationReset;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Resets to nearest target when camera movement exceeds rotation threshold.");
                }

                if (useCameraRotationReset)
                {
                    int rotationPercent = config.RotationPercent[0];
                    if (ImGui.SliderInt("Rotation threshold (% movement)##1", ref rotationPercent, 1, 100))
                    {
                        config.RotationPercent[0] = rotationPercent;
                        configChanged = true;
                    }
                }

                if (useCameraRotationReset)
                {
                    ImGui.Indent();
                    ImGui.Text("Combine with:");

                    bool combineWithNewEntity = config.ResetCombinations[0, 0];
                    if (ImGui.Checkbox("New Target Reset##1", ref combineWithNewEntity))
                    {
                        config.ResetCombinations[0, 0] = combineWithNewEntity;
                        configChanged = true;
                    }

                    bool combineWithProximity = config.ResetCombinations[0, 1];
                    if (ImGui.Checkbox("New Closest Target Reset##1", ref combineWithProximity))
                    {
                        config.ResetCombinations[0, 1] = combineWithProximity;
                        configChanged = true;
                    }
                    ImGui.Unindent();
                }

                bool useNewEntityReset = config.BaseCombatantReset;
                if (ImGui.Checkbox("New Target Reset##2", ref useNewEntityReset))
                {
                    config.BaseCombatantReset = useNewEntityReset;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Resets to nearest target when new entities appear or leave the targeting range.");
                }
                if (useNewEntityReset)
                {
                    ImGui.Indent();
                    ImGui.Text("Combine with:");

                    bool combineWithCamera = config.ResetCombinations[1, 0];
                    if (ImGui.Checkbox("Camera Rotation Reset##2", ref combineWithCamera))
                    {
                        config.ResetCombinations[1, 0] = combineWithCamera;
                        configChanged = true;
                    }

                    if (combineWithCamera)
                    {
                        int rotationPercent = config.RotationPercent[1];
                        if (ImGui.SliderInt("Rotation threshold (% movement)##2", ref rotationPercent, 1, 100))
                        {
                            config.RotationPercent[1] = rotationPercent;
                            configChanged = true;
                        }
                    }

                    bool combineWithProximity = config.ResetCombinations[1, 1];
                    if (ImGui.Checkbox("New Closest Target Reset##2", ref combineWithProximity))
                    {
                        config.ResetCombinations[1, 1] = combineWithProximity;
                        configChanged = true;
                    }
                    ImGui.Unindent();
                }

                bool useProximityReset = config.BaseNewTargetReset;
                if (ImGui.Checkbox("New Closest Target Reset##3", ref useProximityReset))
                {
                    config.BaseNewTargetReset = useProximityReset;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Resets to nearest target when a new closer entity becomes available.");
                }
                if (useProximityReset)
                {
                    ImGui.Indent();
                    ImGui.Text("Combine with:");

                    bool combineWithCamera = config.ResetCombinations[2, 0];
                    if (ImGui.Checkbox("Camera Rotation Reset##3", ref combineWithCamera))
                    {
                        config.ResetCombinations[2, 0] = combineWithCamera;
                        configChanged = true;
                    }

                    if (combineWithCamera)
                    {
                        int rotationPercent = config.RotationPercent[2];
                        if (ImGui.SliderInt("Rotation threshold (% movement)##3", ref rotationPercent, 1, 100))
                        {
                            config.RotationPercent[2] = rotationPercent;
                            configChanged = true;
                        }
                    }

                    bool combineWithNewEntity = config.ResetCombinations[2, 1];
                    if (ImGui.Checkbox("New Target Reset##3", ref combineWithNewEntity))
                    {
                        config.ResetCombinations[2, 1] = combineWithNewEntity;
                        configChanged = true;
                    }
                    ImGui.Unindent();
                }

                ImGui.Spacing();

                ImGui.TextDisabled("Target Filtering Options");
                ImGui.Separator();

                bool stickyTargets = config.StickyTargetOnReset;
                if (ImGui.Checkbox("Sticky Target On Reset", ref stickyTargets))
                {
                    config.StickyTargetOnReset = stickyTargets;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("When enabled, keeps your current target after a reset if that target is still the closest. When disabled, resets to the second closest enemy.");
                }

                bool onlyAttackable = config.OnlyAttackableObjects;
                if (ImGui.Checkbox("Target Only Attackable Objects", ref onlyAttackable))
                {
                    config.OnlyAttackableObjects = onlyAttackable;
                    configChanged = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("When enabled, only attackable Objects will be targeted.");
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

                bool showDebugOptions = config.ShowDebugOptions;
                if (ImGui.Checkbox("Show Debug Options", ref showDebugOptions))
                {
                    config.ShowDebugOptions = showDebugOptions;
                    configChanged = true;
                }

                if (showDebugOptions)
                {
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
                        ImGui.SetTooltip("Advanced setting: Modify only if you understand the implications.");
                        ImGui.PopStyleColor();
                    }
                }

                if (configChanged)
                    config.Save();
            }
            ImGui.End();
        }
    }
}