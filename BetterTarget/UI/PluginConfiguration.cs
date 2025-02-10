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
        public float CameraRadius { get; set; } = 60f;
        public bool ShowDebug { get; set; } = false;
    }
}
