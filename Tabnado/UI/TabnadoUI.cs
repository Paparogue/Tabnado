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
