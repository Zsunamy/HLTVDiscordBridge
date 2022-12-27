using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class MatchStats
{
    public uint StatsId { get; set; }
    public uint MatchId { get; set; }
    public List<uint> MapStatIds { get; set; }
    public ulong Date { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public Event EventObj { get; set; }
    public MatchStatsPlayerTeams MatchStatsPlayerTeams { get; set; }
    public string Link { get; set; }

    public override string ToString()
    {
        return JObject.FromObject(this).ToString();
    }
}