// RPMainWindow.cs
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.ImGuiNotification;

namespace HousePlayerTracker;

public class HPTMainWindow : Window
{
    private uint lastTerritoryId = 0;
    private bool hasNotifiedExit = false;

    private bool isTracking = false;
    private uint currentTerritoryId = 0;

    public HPTMainWindow() : base(
        "RP Attendance Tracker###RPWindow",
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        RespectCloseHotkey = true;
        IsOpen = false;
    }

    public override void Draw()
    {
        currentTerritoryId = Plugin.ClientState.TerritoryType;

        // 显示当前地图 ID
        ImGui.TextUnformatted($"Current Territory ID: {currentTerritoryId}");

        // 追踪按钮逻辑
        if (isTracking)
        {
            ImGui.TextColored(new System.Numerics.Vector4(0f, 1f, 0f, 1f), "Tracking Enabled");
            if (ImGui.Button("Stop Tracking"))
                isTracking = false;
        }
        else
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.5f, 0f, 1f), "Tracking Disabled");
            if (ImGui.Button("Start Tracking"))
                isTracking = true;
        }

        // 追踪状态下判断是否离开地图
        if (lastTerritoryId != 0 && currentTerritoryId != lastTerritoryId)
        {
            if (isTracking && !hasNotifiedExit)
            {
                isTracking = false;
                hasNotifiedExit = true;

                Plugin.Notification.AddNotification(new Notification
                {
                    Content = "You left the house. Tracking stopped.",
                    Type = NotificationType.Warning
                });
            }
        }
        else
        {
            // 玩家在当前地图内，重置通知开关
            hasNotifiedExit = false;
        }

        // 更新最后地图 ID
        lastTerritoryId = currentTerritoryId;

        // 玩家列表展示
        ImGui.Separator();
        ImGui.TextUnformatted("Players currently in this house:");

        var players = new List<IPlayerCharacter>();

        foreach (var obj in Plugin.ObjectTable)
        {
            if (obj.ObjectKind == ObjectKind.Player && obj is IPlayerCharacter player)
            {
                try
                {
                    // 访问这些字段前先确认它们不是 null
                    var name = player?.Name?.TextValue;
                    var job = player?.ClassJob.Value.Abbreviation.ToString();
                    var level = player?.Level;

                    if (player != null && name != null && job != null && level != null)
                        players.Add(player);
                }
                catch
                {
                    // 忽略出错的对象，避免崩整个列表
                }
            }
        }

        players = players.OrderBy(p => p.Name.TextValue).ToList();


        if (players.Count == 0)
        {
            ImGui.TextUnformatted("No players found in this area.");
        }
        else
        {
            if (ImGui.BeginTable("PlayersTable", 3, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Job");
                ImGui.TableSetupColumn("Level");
                ImGui.TableHeadersRow();

                foreach (var p in players)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted(p.Name.TextValue);
                    ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(p.ClassJob.Value.Abbreviation.ToString());
                    ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted(p.Level.ToString());
                }

                ImGui.EndTable();
            }
        }
    }
}
