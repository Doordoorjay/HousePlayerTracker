using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System.IO;
using System.Text.Json;
using Dalamud.Logging;
using HousePlayerTracker;
using System.Collections.Generic;
using System;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Game.Text;

namespace HousePlayerTracker;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "House Player Tracker";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static INotificationManager Notification { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    private WindowSystem windowSystem = new("HousePlayerTracker");
    private HPTMainWindow mainWindow;


    public Plugin()
    {

        mainWindow = new HPTMainWindow();
        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler("/housetrack", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open House Player Tracker window"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // Setup menu direct
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi += OpenMain;
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler("/housetrack");
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

    // OpenConfigUi
    private void OpenConfig()
    {
        mainWindow.IsOpen = true;
    }

    // OpenMainUi
    private void OpenMain()
    {
        mainWindow.IsOpen = true;
    }

    // Load housing area list
    private void LoadHousingZoneList()
    {
        var path = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "houseWhiteList.json");
        if (File.Exists(path))
        {
            try
            {
                var content = File.ReadAllText(path);
                var zoneIds = JsonSerializer.Deserialize<HashSet<uint>>(content);
                if (zoneIds is not null)
                    HPTMainWindow.PublicHousingZoneIds = zoneIds;
            }
            catch (Exception ex)
            {
                Plugin.Notification.AddNotification(new Notification
                {
                    Content = $"Failed to load houseWhiteList.json: {ex.Message}",
                    Type = NotificationType.Error
                });
            }

        }
    }

}
