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
        public int ClearDeadTable { get; set; } = 1000;
        public int CameraRadius { get; set; } = 420;
        public int RotationPercent { get; set; } = 5;
        public int RaycastMultiplier { get; set; } = 4;
        public int RayCastPercent { get; set; } = 100;
        public int VisibilityPercent { get; set; } = 35;
        public float CameraDepth { get; set; } = 1f;
        public int DrawRefreshRate { get; set; } = 5;
        public bool OnlyHostilePlayers { get; set; } = true;
        public bool UseCameraRotationReset { get; set; } = true;
        public bool UseCombatantReset { get; set; } = false;
        public bool UseNewTargetReset { get; set; } = true;
        public bool OnlyBattleNPCs { get; set; } = true;
        public bool OnlyVisibleObjects { get; set; } = true;
        public bool ClearTargetTable { get; set; } = false;
        public bool DrawSelection { get; set; } = false;
        public bool ShowDebugSelection { get; set; } = false;
        public bool ShowDebugRaycast { get; set; } = false;
        public bool ChainResetLogic { get; set; } = false;
        public bool[,] ResetCombinations { get; set; } = new bool[3, 3] {
        { false, false, false },
        { false, false, false },
        { false, false, false }
        };
        public string SelectedKey { get; set; } = "Tab";
    }
}
