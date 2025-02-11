using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Tabnado.UI
{
    [Serializable]
    public class PluginConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface?.SavePluginConfig(this);
        }

        public float MaxTargetDistance { get; set; } = 60f;
        public float CameraRadius { get; set; } = 10f;
        public bool OnlyAttackAbles { get; set; } = true;
        public bool OnlyVisibleObjects { get; set; } = true;
        public bool ShowDebug { get; set; } = false;
    }
}
