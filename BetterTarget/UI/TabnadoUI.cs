using System.Numerics;
using Tabnado.Others;
using Dalamud.Plugin;
using ImGuiNET;

namespace Tabnado.UI
{
    public class TabnadoUI
    {
        private bool settingsVisible = false;
        private PluginConfig config;
        private Others.Tabnado targetingManager;
        private IDalamudPluginInterface pluginInterface;

        public TabnadoUI(IDalamudPluginInterface pluginInterface, PluginConfig config, Others.Tabnado targetingManager)
        {
            this.pluginInterface = pluginInterface;
            this.config = config;
            this.targetingManager = targetingManager;
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

                bool onlyAttackable = config.OnlyAttackAbles;
                if (ImGui.Checkbox("Target Only Attackable Objects", ref onlyAttackable))
                {
                    config.OnlyAttackAbles = onlyAttackable;
                    configChanged = true;
                }

                bool onlyVisibleObjects = config.OnlyVisibleObjects;
                if (ImGui.Checkbox("Target Only Visible Objects", ref onlyVisibleObjects))
                {
                    config.OnlyVisibleObjects = onlyVisibleObjects;
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
            }
            ImGui.End();
        }
    }
}
