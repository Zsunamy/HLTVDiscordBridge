namespace HLTVDiscordBridge.Shared;

public class TeamOverviewStatistics
{
    public uint MapsPlayed { get; set; }
    public uint TotalKills { get; set; }
    public uint TotalDeaths { get; set; }
    public uint RoundsPlayed { get; set; }
    public float KdRatio { get; set; }
    public uint Wins { get; set; }
    public uint Draws { get; set; }
    public uint Losses { get; set; }
}