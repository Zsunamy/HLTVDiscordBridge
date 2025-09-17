namespace HLTVDiscordBridge.Shared;

public class MatchStatsPlayer
{
    public Player Player { get; set; }
    public int Kills { get; set; }
    public int HsKills { get; set; }
    public int Assists { get; set; }
    public int FlashAssists { get; set; }
    public int Deaths { get; set; }
    public int DeathsTraded { get; set; }
    public int OpeningKills { get; set; }
    public int OpeningDeaths { get; set; }
    public int MultiKillRounds { get; set; }
    public int Clutches { get; set; }
    public float KillsPerRound { get; set; }
    public float DeathsPerRound { get; set; }
    public float Rating { get; set; }
    public float RatingVersion { get; set; }
    public float? Impact { get; set; }
    public float? RoundSwings { get; set; }
    public float? MultiKillRating { get; set; }
    public float Adr { get; set; }
    public float Kast { get; set; }
    
}