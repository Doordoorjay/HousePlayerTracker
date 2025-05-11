using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using HousePlayerTracker;

namespace HousePlayerTracker;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "House Player Tracker";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static INotificationManager Notification { get; private set; } = null!;

    private WindowSystem windowSystem = new("HousePlayerTracker");
    private HPTMainWindow mainWindow;

    public Plugin()
    {
        mainWindow = new HPTMainWindow();
        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler("/rpat", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open RP Attendance Tracker window"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // ✅ 添加这两个回调，修复 validation 提示
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi += OpenMain;
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler("/rpat");
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMain;
    }

    private void OnCommand(string command, string args)
    {
        mainWindow.IsOpen = true;
    }

    private void DrawUI()
    {
        windowSystem.Draw();
    }

    // ✅ 用于 OpenConfigUi
    private void OpenConfig()
    {
        mainWindow.IsOpen = true;
    }

    // ✅ 用于 OpenMainUi
    private void OpenMain()
    {
        mainWindow.IsOpen = true;
    }
}
