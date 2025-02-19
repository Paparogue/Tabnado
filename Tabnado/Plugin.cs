using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Tabnado.UI;
using Tabnado.Util;

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

        public PluginConfig PluginConfig;
        public TargetingController TabController;
        public TabnadoUI TabnadoUI;
        public CameraScene CameraScene;
        public KeyDetection KeyDetection;

        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            PluginConfig = PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
            PluginConfig.Initialize(PluginInterface);

            KeyDetection = new KeyDetection();
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

        public void Dispose()
        {
            CommandManager.RemoveHandler("/tabnado");
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenMainUi -= OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OnToggleUI;
        }

        private void OnToggleUI(string command, string args)
        {
            TabnadoUI.ToggleVisibility();
        }

        private void OnToggleUI()
        {
            OnToggleUI(null!, null!);
        }
        private void OnDraw()
        {
            if (ClientState is not null && ClientState.LocalPlayer is not null)
            {
                TabController.Draw();
                TabnadoUI.Draw();
            }
        }
    }
}
