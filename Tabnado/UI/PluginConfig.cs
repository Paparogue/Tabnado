using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Tabnado.UI
{
    [Serializable]
    public class PluginConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 4;

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
        public int ClearTargetTableTimer { get; set; } = 1000;
        public int CameraRadius { get; set; } = 375;
        public int[] RotationPercent { get; set; } = new int[3] { 5, 5, 5 };
        public int RaycastMultiplier { get; set; } = 4;
        public int RayCastPercent { get; set; } = 100;
        public int VisibilityPercent { get; set; } = 35;
        public float CameraDepth { get; set; } = 1f;
        public float DistanceLerp { get; set; } = 5f;
        public float CameraLerp { get; set; } = 0.01f;
        public float MonitorX { get; set; } = 50f;
        public float MonitorY { get; set; } = 42.5f;
        public int DrawRefreshRate { get; set; } = 5;
        public bool BaseCameraReset { get; set; } = true;
        public bool BaseCombatantReset { get; set; } = false;
        public bool BaseNewTargetReset { get; set; } = true;
        public bool StickyTargetOnReset { get; set; } = false;
        public bool OnlyAttackableObjects { get; set; } = true;
        //public bool OnlyBattleNPCs { get; set; } = true;
        public bool OnlyVisibleObjects { get; set; } = true;
        public bool ClearTargetTable { get; set; } = false;
        public bool ShowDebugSelection { get; set; } = false;
        public bool ShowDebugRaycast { get; set; } = false;
        public bool UseCameraLerp { get; set; } = true;
        public bool UseDistanceLerp { get; set; } = true;
        public bool AlternativeTargeting { get; set; } = true;
        public bool ResetOnNoTarget { get; set; } = true;
        public bool ShowDebugOptions { get; set; } = false;
        public bool[,] ResetCombinations { get; set; } = new bool[3, 2] {
        { false, false },
        { false, false },
        { false, false }
        };
        public string SelectedKey { get; set; } = "Tab";
    }
}
