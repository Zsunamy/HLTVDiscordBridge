using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class MapResult
{
    public string Team1TotalRounds { get; set; }
    public string Team2TotalRounds { get; set; }
    public List<MapHalfResult> MapHalfResults { get; set; }
}