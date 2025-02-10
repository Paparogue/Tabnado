using System.Numerics;
using BetterTarget.UI;
using Dalamud.Plugin;
using ImGuiNET;

namespace BetterTarget
{
    public class SmartTabTargetingUI
    {
        private bool settingsVisible = false;
        private PluginConfiguration config;
        private SmartTabTargetingManager targetingManager;
        private IDalamudPluginInterface pluginInterface;

        public SmartTabTargetingUI(IDalamudPluginInterface pluginInterface, PluginConfiguration config, SmartTabTargetingManager targetingManager)
        {
            this.pluginInterface = pluginInterface;
            this.config = config;
            this.targetingManager = targetingManager;
        }

        /// <summary>
        /// Toggles the visibility of the settings window.
        /// </summary>
        public void ToggleVisibility()
        {
            settingsVisible = !settingsVisible;
        }

        /// <summary>
        /// Draws the settings window.
        /// </summary>
        public void Draw()
        {
            if (!settingsVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Smart Tab Target Settings", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                bool configChanged = false;

                // Use local variables for the config properties (since properties cannot be passed by ref).
                float maxTargetDistance = config.MaxTargetDistance;
                if (ImGui.SliderFloat("Max Target Distance", ref maxTargetDistance, 10f, 100f, "%.1f"))
                {
                    config.MaxTargetDistance = maxTargetDistance;
                    configChanged = true;
                }

                float overrideFOV = config.OverrideFieldOfView;
                if (ImGui.SliderFloat("Camera Field of View", ref overrideFOV, 30f, 120f, "%.1f"))
                {
                    config.OverrideFieldOfView = overrideFOV;
                    configChanged = true;
                }

                float distanceWeight = config.DistanceWeight;
                if (ImGui.SliderFloat("Distance Weight", ref distanceWeight, 0f, 5f, "%.2f"))
                {
                    config.DistanceWeight = distanceWeight;
                    configChanged = true;
                }

                float alignmentWeight = config.AlignmentWeight;
                if (ImGui.SliderFloat("Alignment Weight", ref alignmentWeight, 0f, 5f, "%.2f"))
                {
                    config.AlignmentWeight = alignmentWeight;
                    configChanged = true;
                }

                bool enableTargetCycling = config.EnableTargetCycling;
                if (ImGui.Checkbox("Enable Target Cycling", ref enableTargetCycling))
                {
                    config.EnableTargetCycling = enableTargetCycling;
                    configChanged = true;
                }

                float cycleTimeout = config.CycleTimeout;
                if (ImGui.SliderFloat("Cycle Timeout (sec)", ref cycleTimeout, 0.5f, 5f, "%.1f"))
                {
                    config.CycleTimeout = cycleTimeout;
                    configChanged = true;
                }

                float aggroWeight = config.AggroWeight;
                if (ImGui.SliderFloat("Aggro Weight", ref aggroWeight, 0f, 3f, "%.2f"))
                {
                    config.AggroWeight = aggroWeight;
                    configChanged = true;
                }

                float typeWeight = config.TypeWeight;
                if (ImGui.SliderFloat("Type Weight", ref typeWeight, 0f, 3f, "%.2f"))
                {
                    config.TypeWeight = typeWeight;
                    configChanged = true;
                }

                bool showDebug = config.ShowDebug;
                if (ImGui.Checkbox("Show Debug Info", ref showDebug))
                {
                    config.ShowDebug = showDebug;
                    configChanged = true;
                }

                if (configChanged)
                    config.Save();

                if (config.ShowDebug)
                {
                    ImGui.Separator();
                    ImGui.Text("Candidate Targets:");
                    foreach (var candidate in targetingManager.LastCandidateList)
                    {
                        // Get a displayable name.
                        string candidateName = candidate.Target.Name?.ToString() ?? "Unknown";
                        ImGui.Text($"{candidateName}: Score={candidate.Score:F2}, Distance={candidate.Distance:F1}, Angle={candidate.AngleDegrees:F1}");
                    }
                }
            }
            ImGui.End();
        }
    }
}
