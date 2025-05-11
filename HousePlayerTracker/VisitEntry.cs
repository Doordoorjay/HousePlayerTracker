using System;

namespace HousePlayerTracker;

public class VisitEntry
{
    public string Name { get; set; } = string.Empty;
    public DateTime EnteredAt { get; set; }
    public DateTime? LeftAt { get; set; } = null;
    public uint TerritoryId { get; set; }
}
