﻿using Dalamud.Configuration;
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

        public int MaxTargetDistance { get; set; } = 50;
        public int CameraRadius { get; set; } = 300;
        public bool OnlyAttackAbles { get; set; } = true;
        public bool OnlyVisibleObjects { get; set; } = true;
        public bool ShowDebugSelection { get; set; } = false;
        public bool ShowDebugRaycast { get; set; } = false;
    }
}
