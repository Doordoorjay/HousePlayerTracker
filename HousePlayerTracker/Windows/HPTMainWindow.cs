using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ImGuiNET;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using System.IO;
using System.Text;
using System.Globalization;
using Dalamud.Game.Text;

namespace HousePlayerTracker;

public class HPTMainWindow : Window
{
    private uint lastTerritoryId = 0;
    private uint lastZonePrompted = 0;
    private bool showTrackingWarning = false;
    private bool hasNotifiedExit = false;

    private bool isTracking = false;
    private bool ignoreZone = false;
    private uint currentTerritoryId = 0;

    private readonly Dictionary<string, VisitEntry> activeVisits = new();
    private readonly List<VisitEntry> visitHistory = new();
    public static HashSet<uint> PublicHousingZoneIds = new();

    public HPTMainWindow() : base(
        "House Player Tracker###HPTWindow",
        ImGuiWindowFlags.NoCollapse)
    {
        RespectCloseHotkey = true;
        IsOpen = false;
    }

    public class VisitEntry
    {
        public string Name { get; set; } = "";
        public DateTime EnteredAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public uint TerritoryId { get; set; }
        public int EntryCount { get; set; } = 1;

        public TimeSpan? Duration => LeftAt.HasValue ? LeftAt.Value - EnteredAt : null;
    }

    public override void Draw()
    {
        currentTerritoryId = Plugin.ClientState.TerritoryType;
        ImGui.TextUnformatted($"Current Territory ID: {currentTerritoryId}");

        // Warn user if they are not in the house zone whitelist
        if (!PublicHousingZoneIds.Contains(currentTerritoryId))
        {
            ImGui.TextColored(new Vector4(1f, 0.6f, 0f, 1f), "Warning: You are in a public area, and it might spam the list.");
        }

        CheckTrackingOutsideHousingZone();
        DrawTrackingToggle();
        HandleTerritoryChange();

        ImGui.Separator();
        DrawPlayerList();
        ImGui.Separator();
        DrawVisitHistory();
        DrawTrackingPopup();
    }


    private void CheckTrackingOutsideHousingZone()
    {
        uint currentZoneId = Plugin.ClientState.TerritoryType;

        if (isTracking || !IsOpen)
            return;

        if (!PublicHousingZoneIds.Contains(currentZoneId) && currentZoneId != lastZonePrompted)
        {
            lastZonePrompted = currentZoneId;
            showTrackingWarning = true;
            ImGui.OpenPopup("TrackingWarningPopup");
        }
        if (showTrackingWarning)
        {
            ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), "Tracking will stop after exiting the zone.");
        }
    }

    private void DrawTrackingPopup()
    {
        if (ImGui.BeginPopup("TrackingWarningPopup"))
        {
            ImGui.TextWrapped("You are not in a housing area.");
            ImGui.TextWrapped("Would you like to continue viewing this plugin window?");

            if (ImGui.Button("Yes"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("No"))
            {
                IsOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

private void DrawTrackingToggle()
{
    if (isTracking)
    {
        ImGui.TextColored(new Vector4(0f, 1f, 0f, 1f), "Tracking Enabled");
        if (ImGui.Button("Stop Tracking"))
        {
            Plugin.Chat.Print("Player tracking manually stopped.");
            Plugin.Notification.AddNotification(new Notification
            {
                Content = "Track stopped",
                Type = NotificationType.Info
            });

            foreach (var entry in activeVisits.Values)
            {
                entry.LeftAt ??= DateTime.Now;

                var existing = visitHistory.FirstOrDefault(v => v.Name == entry.Name);
                if (existing == null)
                {
                    visitHistory.Add(entry);
                }
                else
                {
                    if (entry.EnteredAt < existing.EnteredAt)
                        existing.EnteredAt = entry.EnteredAt;

                    if (entry.LeftAt > existing.LeftAt)
                        existing.LeftAt = entry.LeftAt;

                    existing.EntryCount = entry.EntryCount;
                }
            }

            activeVisits.Clear();
            StopTracking();
        }
    }
    else
    {
        if (ImGui.Button("Start Tracking"))
            isTracking = true;
            Plugin.Chat.Print("Player tracking started.");

        ImGui.TextColored(new Vector4(1f, 0.5f, 0f, 1f), "Tracking Disabled");
    }

    // Toggle auto-stop tracking when leave zone?
    ImGui.Checkbox("Do not stop tracking when changing zone", ref ignoreZone);
}

    private void StopTracking()
    {
        foreach (var entry in activeVisits.Values)
        {
            entry.LeftAt ??= DateTime.Now;

            // Only add entry counts from active player if did not exist in history
            if (!visitHistory.Any(v => v.Name == entry.Name && v.EnteredAt == entry.EnteredAt))
            {
                visitHistory.Add(entry);
            }
        }

        activeVisits.Clear();
        isTracking = false;
    }

    // If player changed zone and they're in tracking status with checkbox = true
    private void HandleTerritoryChange()
    {
        if (lastTerritoryId != 0 && currentTerritoryId != lastTerritoryId && isTracking && !hasNotifiedExit && !ignoreZone)
        {
            // Tag player leaved time
            foreach (var entry in activeVisits.Values)
            {
                entry.LeftAt = DateTime.Now;
            }

            StopTracking();

            hasNotifiedExit = true;
            Plugin.Chat.Print("You left the zone. Player Tracking was automatically stopped.");
        }
        else if (currentTerritoryId == lastTerritoryId)
        {
            hasNotifiedExit = false;
        }

        lastTerritoryId = currentTerritoryId;
    }


    private void DrawPlayerList()
    {
        ImGui.TextUnformatted("Players currently in this zone:");

        // Exclude myself
        var localName = Plugin.ClientState.LocalPlayer?.Name.TextValue;

        var players = Plugin.ObjectTable
            .Where(obj => obj.ObjectKind == ObjectKind.Player && obj is IPlayerCharacter)
            .OfType<IPlayerCharacter>()
            .Where(p => p.Name.TextValue != null && p.Name.TextValue != localName)
            .OrderBy(p => p.Name.TextValue)
            .ToList();


        if (isTracking)
        {
            if (players.Count == 0)
            {
                ImGui.TextUnformatted("No players found in this area.");
                return;
            }

            if (ImGui.BeginTable("PlayersTable", 4, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Status");
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Job");
                ImGui.TableSetupColumn("Level");
                ImGui.TableHeadersRow();

                foreach (var p in players)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0); DrawStatusIcon(p);
                    ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(p.Name.TextValue);
                    ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted(p.ClassJob.Value.Abbreviation.ToString());
                    ImGui.TableSetColumnIndex(3); ImGui.TextUnformatted(p.Level.ToString());
                }

                ImGui.EndTable();
            }

            UpdateVisitTracking(players);
        }
        else
        {
            ImGui.TextUnformatted("Tracking not started.");
        }
    }

    private void UpdateVisitTracking(List<IPlayerCharacter> players)
    {
        var currentPlayers = players.Select(p => p.Name.TextValue).ToHashSet();

        // 添加新进来的玩家（如果之前没有或之前已经离开）
        foreach (var name in currentPlayers)
        {
            // 已经在列表中，且尚未离开，则不处理
            if (activeVisits.TryGetValue(name, out var visit) && visit.LeftAt == null)
                continue;

            // 重新进来或首次进来：更新 entry count（从 history 中查找旧 count）
            int currentCount = visitHistory.FirstOrDefault(v => v.Name == name)?.EntryCount ?? 0;

            activeVisits[name] = new VisitEntry
            {
                Name = name,
                EnteredAt = DateTime.Now,
                TerritoryId = currentTerritoryId,
                EntryCount = currentCount + 1
            };
        }

        // Leaved players
        var leftPlayers = activeVisits.Keys.Where(name => !currentPlayers.Contains(name)).ToList();
        foreach (var name in leftPlayers)
        {
            if (activeVisits.TryGetValue(name, out var entry))
            {
                entry.LeftAt = DateTime.Now;

                var existing = visitHistory.FirstOrDefault(v => v.Name == name);
                if (existing == null)
                {
                    visitHistory.Add(entry);
                }
                else
                {
                    if (entry.EnteredAt < existing.EnteredAt)
                        existing.EnteredAt = entry.EnteredAt;
                    if (entry.LeftAt > existing.LeftAt)
                        existing.LeftAt = entry.LeftAt;
                    existing.EntryCount = entry.EntryCount;
                }

                activeVisits.Remove(name); // Clean them
            }
        }

    }



    private void DrawVisitHistory()
    {
        ImGui.TextUnformatted("Visit History:");

        if (visitHistory.Count == 0)
        {
            ImGui.TextUnformatted("No history visits recorded.");
            return;
        }

        if (ImGui.BeginTable("VisitTable", 5, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("First Entered");
            ImGui.TableSetupColumn("Last Left");
            ImGui.TableSetupColumn("Duration");
            ImGui.TableSetupColumn("Entry Count");
            ImGui.TableHeadersRow();

            foreach (var v in visitHistory.OrderBy(v => v.EnteredAt))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted(v.Name);
                ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(v.EnteredAt.ToString("HH:mm:ss"));
                ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted(v.LeftAt?.ToString("HH:mm:ss") ?? "-");
                ImGui.TableSetColumnIndex(3); ImGui.TextUnformatted(v.Duration?.ToString(@"hh\:mm\:ss") ?? "-");
                ImGui.TableSetColumnIndex(4); ImGui.TextUnformatted(v.EntryCount.ToString());
            }

            ImGui.EndTable();
        }

        if (ImGui.Button("Clear History"))
        {
            activeVisits.Clear();
            visitHistory.Clear();
        }

        ImGui.SameLine();

        // CSV Exports
        if (ImGui.Button("Export CSV"))
        {

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"visitEntry_{timestamp}.csv";
            var path = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, filename);

            var sb = new StringBuilder();
            sb.AppendLine("Name,Entered At,Left At,Duration,Entry Count");

            foreach (var v in visitHistory.OrderBy(v => v.EnteredAt))
            {
                var durationFormatted = v.Duration?.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture) ?? "-";
                var line = $"{v.Name},{v.EnteredAt:yyyy-MM-dd HH:mm:ss},{v.LeftAt:yyyy-MM-dd HH:mm:ss},{durationFormatted},{v.EntryCount}";
                sb.AppendLine(line);
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));

            Plugin.Chat.Print($"Exported {visitHistory.Count} entries to: {path}");
        }

    }

    private void DrawStatusIcon(IPlayerCharacter player)
    {
        try
        {
            string? statusName = null;

            // Sometimes user online status will be (none)
            if (player.OnlineStatus.RowId != 0)
                statusName = player.OnlineStatus.Value.Name.ToString();

            uint iconId = 61505;

            if (!string.IsNullOrEmpty(statusName) && StatusIconMap.TryGetValue(statusName, out var mapped))
                iconId = mapped;

            var tex = Plugin.TextureProvider
                .GetFromGameIcon(iconId)
                .GetWrapOrEmpty()
                .ImGuiHandle;

            if (tex != IntPtr.Zero)
            {
                ImGui.Image(tex, new Vector2(18, 18) * ImGuiHelpers.GlobalScale);

                if (!string.IsNullOrEmpty(statusName) && ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(statusName);
                    ImGui.EndTooltip();
                }
            }
        }
        catch
        {
        }
    }

    private static readonly Dictionary<string, uint> StatusIconMap = new()
    {
        { "Disconnected", 61503 }, { "网络断开", 61503 },
        { "Away from Keyboard", 61511 }, { "离开", 61511 },
        { "Busy", 61509 }, { "忙碌", 61509 },
        { "Viewing Cutscene", 61508 }, { "动画中", 61508 },
        { "Waiting for Duty Finder", 61517 }, { "任务搜索申请中", 61517 },
        { "Mentor", 61540 }, { "指导者", 61540 },
        { "Battle Mentor", 61542 }, { "战斗指导者", 61542 },
        { "PvE Mentor", 61542 }, { "PvE 指导者", 61542 },
        { "PvP Mentor", 61544 }, { "对战指导者", 61544 },
        { "Trade Mentor", 61543 }, { "制作采集指导者", 61543 },
        { "Returner", 61537 }, { "回归者", 61537 },
        { "New Adventurer", 61523 }, { "新人", 61523 },
        { "Party Leader", 61521 }, { "小队队长", 61521 },
        { "Party Member", 61522 }, { "小队队员", 61522 },
        { "Party Leader (Cross-world)", 61961 }, { "跨服小队队长", 61961 },
        { "Party Member (Cross-world)", 61962 }, { "跨服小队队员", 61962 },
        { "Recruiting Party Members", 61536 }, { "队员招募中", 61536 },
        { "Looking for Party", 61515 }, { "希望组队", 61515 },
        { "Role-playing", 61545 }, { "角色扮演中", 61545 },
        { "Camera Mode", 61546 }, { "观景模式中", 61546 },
        { "Online", 61505 }, { "在线", 61505 } // This is basically useless but keep here for reference
    };
}
