using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class MatchMapStats
{
    public uint Id { get; set; }
    public uint MatchId { get; set; }
    public MapResult Result { get; set; }
    public string Map { get; set; }
    public ulong Date { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public Event EventObj { get; set; }
    public List<MatchMapStatsPlayer> Team1Stats { get; set; }
    public List<MatchMapStatsPlayer> Team2Stats { get; set; }
    public string Link { get; set; }
}