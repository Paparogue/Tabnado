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

        public int MaxTargetDistance { get; set; } = 55;
        public int CameraRadius { get; set; } = 400;
        public bool OnlyHostilePlayers { get; set; } = true;
        public bool OnlyBattleNPCs { get; set; } = true;
        public bool OnlyVisibleObjects { get; set; } = true;
        public bool ShowDebugSelection { get; set; } = false;
        public bool ShowDebugRaycast { get; set; } = false;
        public string SelectedKey { get; set; } = "Tab";
    }
}
