using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OnionHunter.Services;
using OnionHunter.Windows;

namespace OnionHunter;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    private const string Command = "/onionhunt";

    public Configuration Config { get; }

    private readonly WindowSystem _windows = new("OnionHunter");
    private readonly MainWindow _main;
    private readonly VentureLogger _logger;

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _main = new MainWindow(this);
        _windows.AddWindow(_main);

        _logger = new VentureLogger(this);
        _logger.Enable();

        Commands.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Onion Hunter venture-odds tracker.",
        });

        PluginInterface.UiBuilder.Draw += _windows.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OpenMain;
        PluginInterface.UiBuilder.OpenConfigUi += OpenMain;
    }

    private void OnCommand(string command, string args) => _main.Toggle();
    private void OpenMain() => _main.IsOpen = true;

    public void Dispose()
    {
        _logger.Dispose();

        PluginInterface.UiBuilder.Draw -= _windows.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMain;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenMain;
        _windows.RemoveAllWindows();

        Commands.RemoveHandler(Command);
    }
}