using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.ImGuiNotification;

using ImGuiNET;

namespace HousePlayerTracker;
using HousePlayerTracker;

public class HPTSettings : Window
{
    public HPTSettings(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow)
    {
    }

    public override void Draw()
    {
        throw new System.NotImplementedException();
    }

    private void Settings()
    {

    }
}