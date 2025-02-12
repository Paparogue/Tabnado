using Tabnado.Objects;
using Tabnado.Others;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

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
        private Others.Tabnado targetingManager;
        private TabnadoUI targetingUI;
        private CameraUtil cameraEnemyList;
        private KeyDetection keyDetector;

        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            PluginConfig = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
            PluginConfig.Initialize(PluginInterface);

            keyDetector = new KeyDetection();
            cameraEnemyList = new CameraUtil(ObjectTable, gameGui, ClientState, PluginConfig, pluginLog);
            targetingManager = new Others.Tabnado(ClientState, ObjectTable, TargetManager, ChatGui, PluginConfig, cameraEnemyList, gameGui, pluginLog, keyDetector);
            targetingUI = new TabnadoUI(PluginInterface, PluginConfig, targetingManager, keyDetector);
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
            targetingUI.ToggleVisibility();
        }

        private void OnToggleUI()
        {
            OnToggleUI(null, null);
        }
        private void OnDraw()
        {
            targetingManager.Draw();
            targetingUI.Draw();
        }
    }
}
