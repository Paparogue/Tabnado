using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Tabnado.Util;

namespace Tabnado.UI
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
        public IChatGui ChatGui { get; set; } = null!;
        [PluginService]
        public IGameGui gameGui { get; set; } = null!;
        [PluginService]
        public IPluginLog pluginLog { get; set; } = null!;

        private PluginConfig PluginConfig;
        private Tabnado tabnado;
        private TabnadoUI tabnadoUI;
        private CameraScene cameraUtil;
        private KeyDetection keyDetection;

        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            PluginConfig = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
            PluginConfig.Initialize(PluginInterface);

            keyDetection = new KeyDetection();
            cameraUtil = new CameraScene(ObjectTable, gameGui, ClientState, PluginConfig, pluginLog);
            tabnado = new Tabnado(ClientState, ObjectTable, TargetManager, ChatGui, PluginConfig, cameraUtil, gameGui, pluginLog, keyDetection);
            tabnadoUI = new TabnadoUI(PluginInterface, PluginConfig, tabnado, keyDetection);
            CommandManager.AddHandler("/tabnado", new CommandInfo(OnToggleUI)
            {
                HelpMessage = "Toggles the Tabnado settings window."
            });

            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenMainUi += OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi += OnToggleUI;

        }

        public void Dispose()
        {
            CommandManager.RemoveHandler("/tabnado");
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenMainUi -= OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OnToggleUI;
        }

        private void OnToggleUI(string command, string args)
        {
            tabnadoUI.ToggleVisibility();
        }

        private void OnToggleUI()
        {
            OnToggleUI(null, null);
        }
        private void OnDraw()
        {
            tabnado.Draw();
            tabnadoUI.Draw();
        }
    }
}
