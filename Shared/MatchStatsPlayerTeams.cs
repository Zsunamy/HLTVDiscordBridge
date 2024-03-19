using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class MatchStatsPlayerTeams
{
    public List<MatchStatsPlayer> Team1PlayerStats { get; set; }
    public List<MatchStatsPlayer> Team2PlayerStats { get; set; }
}