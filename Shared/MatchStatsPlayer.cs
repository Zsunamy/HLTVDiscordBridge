namespace HLTVDiscordBridge.Shared;

public class MatchMapStatsPlayer
{
    public Player Player { get; set; }
    public int Kills { get; set; }
    public int HsKills { get; set; }
    public int Assists { get; set; }
    public int FlashAssists { get; set; }
    public int Deaths { get; set; }
    public float Kast { get; set; }
    public int KillDeathsDifference { get; set; }
    public float Adr { get; set; }
    public int FirstKillsDifference { get; set; }
    public float Rating1 { get; set; }
}