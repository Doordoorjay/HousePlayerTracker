using System;
using System.IO;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.Game.Text;

namespace HousePlayerTracker;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string ExportDirectoryPath { get; set; } = string.Empty;

    [NonSerialized] private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pi)
    {
        pluginInterface = pi;
    }

    public string GetResolvedExportPath()
    {
        if (string.IsNullOrWhiteSpace(ExportDirectoryPath))
            return pluginInterface?.ConfigDirectory.FullName ?? "./";

        return ExportDirectoryPath;
    }

    public void Save()
    {
        pluginInterface?.SavePluginConfig(this);
    }
}
