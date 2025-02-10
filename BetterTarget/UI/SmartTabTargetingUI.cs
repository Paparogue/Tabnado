using System.Numerics;
using BetterTarget.Others;
using Dalamud.Plugin;
using ImGuiNET;

namespace BetterTarget.UI
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

                float maxTargetDistance = config.MaxTargetDistance;
                if (ImGui.SliderFloat("Max Target Distance", ref maxTargetDistance, 1f, 100f, "%.1f"))
                {
                    config.MaxTargetDistance = maxTargetDistance;
                    configChanged = true;
                }

                float cameraRadius = config.CameraRadius;
                if (ImGui.SliderFloat("Camera Search Radius", ref cameraRadius, 1f, 100f, "%.1f"))
                {
                    config.CameraRadius = cameraRadius;
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
