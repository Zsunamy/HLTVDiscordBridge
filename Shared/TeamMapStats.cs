namespace HLTVDiscordBridge.Shared;

public class TeamMapStats
{
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public float WinRate { get; set; }
    public int TotalRounds { get; set; }
    public float RoundWinPAfterFirstKill { get; set; }
    public float RoundWinPAfterFirstDeath { get; set; }
}