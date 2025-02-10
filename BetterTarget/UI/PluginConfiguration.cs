using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace BetterTarget.UI
{
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        /// <summary>
        /// Initializes the configuration with the plugin interface.
        /// </summary>
        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        /// <summary>
        /// Saves the configuration using the latest Dalamud API.
        /// </summary>
        public void Save()
        {
            pluginInterface?.SavePluginConfig(this);
        }

        public float MaxTargetDistance { get; set; } = 30f;
        public float OverrideFieldOfView { get; set; } = 60f;
        public float DistanceWeight { get; set; } = 1f;
        public float AlignmentWeight { get; set; } = 1f;

        public bool EnableTargetCycling { get; set; } = true;
        public float CycleTimeout { get; set; } = 2f;   // Seconds.
        public float AggroWeight { get; set; } = 0f;    // Extra multiplier if aggroed.
        public float TypeWeight { get; set; } = 0f;     // Extra multiplier for target type.
        public bool ShowDebug { get; set; } = false;
    }
}
