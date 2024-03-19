namespace HLTVDiscordBridge.Shared;

public class TeamMapResult
{
    public ulong Date { get; set; }
    public Event EventObj { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public string Map { get; set; }
    public uint MapStatsId { get; set; }
    public ResultResult Result { get; set; }
}