namespace HLTVDiscordBridge.Shared;

public class PlayerOverviewStatistics
{
    public int Kills { get; set; }
    public float Headshots { get; set; }
    public int Deaths { get; set; }
    public float KdRatio { get; set; }
    public float DamagePerRound { get; set; }
    public float GrenadeDamagePerRound { get; set; }
    public int MapsPlayed { get; set; }
    public int RoundsPlayed { get; set; }
    public float KillsPerRound { get; set; }
    public float AssistsPerRound { get; set; }
    public float DeathsPerRound { get; set; }
    public float SavedByTeammatePerRound { get; set; }
    public float SavedTeammatesPerRound { get; set; }
    public float Rating2 { get; set; }
}