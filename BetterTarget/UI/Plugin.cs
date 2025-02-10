using BetterTarget.Objects;
using BetterTarget.Others;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace BetterTarget.UI
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Smart Tab Target Plus";

        [PluginService]
        public IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService]
        public ICommandManager CommandManager { get; set; } = null!;
        [PluginService]
        public IClientState ClientState { get; set; } = null!;
        // Use ObjectTable instead of the old GameObjectManager.
        [PluginService]
        public IObjectTable ObjectTable { get; set; } = null!;
        // Use the read-only ITargetManager.
        [PluginService]
        public ITargetManager TargetManager { get; set; } = null!;
        [PluginService]
        public IChatGui ChatGui { get; set; } = null!;
        [PluginService]
        public IGameGui gameGui { get; set; } = null!;

        // Configuration and helper classes.
        private PluginConfiguration config;
        private SmartTabTargetingManager targetingManager;
        private SmartTabTargetingUI targetingUI;
        private Camera2Enemy cameraEnemyList;

        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            // Load configuration (or create a new one) and initialize it.
            config = PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            config.Initialize(PluginInterface);

            // Initialize the targeting manager and UI.
            cameraEnemyList = new Camera2Enemy(ObjectTable, gameGui);
            targetingManager = new SmartTabTargetingManager(ClientState, ObjectTable, TargetManager, ChatGui, config);
            targetingUI = new SmartTabTargetingUI(PluginInterface, config, targetingManager);
            CommandManager.AddHandler("/smarttabui", new CommandInfo(OnToggleUI)
            {
                HelpMessage = "Toggles the Smart Tab Target settings window."
            });

            // Register UI drawing callback.
            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenMainUi += OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi += OnToggleUI;

        }

        public void Dispose()
        {
            CommandManager.RemoveHandler("/smarttabui");
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenMainUi -= OnToggleUI;
            PluginInterface.UiBuilder.OpenConfigUi -= OnToggleUI;
        }

        /// <summary>
        /// Command handler for /smarttab.
        /// </summary>
        private void OnSmartTab(string command, string args)
        {
            targetingManager.GetSmartTabCandidate();
        }

        /// <summary>
        /// Command handler for /smarttabui.
        /// </summary>
        private void OnToggleUI(string command, string args)
        {
            targetingUI.ToggleVisibility();
        }

        private void OnToggleUI()
        {
            OnToggleUI(null, null);
        }

        /// <summary>
        /// Draw callback for the UI.
        /// </summary>
        private void OnDraw()
        {
            targetingUI.Draw();
        }
    }
}
