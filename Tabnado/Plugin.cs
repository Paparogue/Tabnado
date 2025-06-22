using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Tabnado.Hooks;
using Tabnado.UI;
using Tabnado.Util;
using System;
using Dalamud.Game;

namespace Tabnado
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Tabnado";

        [PluginService]
        public IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService]
        public ICommandManager CommandManager { get; set; } = null!;
        [PluginService]
        public IClientState ClientState { get; set; } = null!;
        [PluginService]
        public IObjectTable ObjectTable { get; set; } = null!;
        [PluginService]
        public ITargetManager TargetManager { get; set; } = null!;
        [PluginService]
        public IChatGui ChatGUI { get; set; } = null!;
        [PluginService]
        public IGameGui GameGUI { get; set; } = null!;
        [PluginService]
        public IPluginLog Log { get; set; } = null!;
        [PluginService]
        public IGameInteropProvider GameInteropProvider { get; set; } = null!;
        [PluginService]
        public ISigScanner SigScanner { get; set; } = null!;

        public PluginConfig PluginConfig;
        public TargetingController TabController;
        public TabnadoUI TabnadoUI;
        public CameraScene CameraScene;
        public TargetingHook TargetingHook { get; private set; } = null!;

        private DateTime lastCameraCheckTime = DateTime.MinValue;
        private const int CAMERA_CHECK_INTERVAL_MS = 50;

        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            PluginConfig = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
            if (PluginInterface.GetPluginConfig() != null && PluginConfig.Version != 5)
                PluginConfig = new PluginConfig();
            PluginConfig.Initialize(PluginInterface);
            PluginConfig.Save();

            TargetingHook = new TargetingHook(Log, GameInteropProvider, SigScanner);

            TargetingHook.OnTargetingFunction += OnTargetingFunctionCalled;

            TargetingHook.IsEnabled = true;
            TargetingHook.BlockOriginalCall = true;

            CameraScene = new CameraScene(this);

            TabController = new TargetingController(this);
            TabnadoUI = new TabnadoUI(this);

            CommandManager.AddHandler("/tabnado", new CommandInfo(OnToggleUI)
            {
                HelpMessage = "Toggles the Tabnado settings window."
            });

            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenMainUi += OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi += OnToggleUI;
        }

        private void OnTargetingFunctionCalled(IntPtr a1, IntPtr a2, IntPtr a3, byte a4, ref bool allowOriginal)
        {
            try
            {
                allowOriginal = false;
                TabController.TargetFunc();
                Log.Verbose("Replaced original targeting function with TabController.TargetFunc()");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in custom targeting function: {ex}");
                allowOriginal = true;
            }
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler("/tabnado");
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenMainUi -= OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OnToggleUI;
            TargetingHook?.Dispose();
        }

        private void OnToggleUI(string command, string args)
        {
            TabnadoUI.ToggleVisibility();
        }

        private void OnToggleUI()
        {
            OnToggleUI(null!, null!);
        }

        private void CameraMatrixDraw()
        {
            var currentTime = DateTime.Now;
            if ((currentTime - lastCameraCheckTime).TotalMilliseconds >= CAMERA_CHECK_INTERVAL_MS)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (CameraScene.CameraExceedsRotation(PluginConfig.RotationPercent[i], i, false))
                        TabController.cameraFlag[i] = true;
                }

                lastCameraCheckTime = currentTime;
            }
        }

        private void OnDraw()
        {
            if (ClientState is not null && ClientState.LocalPlayer is not null)
            {
                if (PluginConfig.ShowDebugRaycast || PluginConfig.ShowDebugSelection)
                    CameraScene.UpdateSceneList();

                if (PluginConfig.ShowDebugRaycast)
                    CameraScene.DrawDebugRaycast();

                if (PluginConfig.ShowDebugSelection)
                    TabController.ShowDebugSelection();

                CameraMatrixDraw();
                TabnadoUI.Draw();
            }
        }
    }
}