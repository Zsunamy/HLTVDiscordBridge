namespace HLTVDiscordBridge.Shared;

public class PlayerIndividualStatistics
{
    public int RoundsWithKills { get; set; }
    public int ZeroKillRounds { get; set; }
    public int OneKillRounds { get; set; }
    public int TwoKillRounds { get; set; }
    public int ThreeKillRounds { get; set; }
    public int FourKillRounds { get; set; }
    public int FiveKillRounds { get; set; }
    public int OpeningKills { get; set; }
    public int OpeningDeaths { get; set; }
    public float OpeningKillRatio { get; set; }
    public float OpeningKillRating { get; set; }
    public float TeamWinPercentAfterFirstKill { get; set; }
    public float FirstKillInWonRounds { get; set; }
    public int RifleKills { get; set; }
    public int SniperKills { get; set; }
    public int SmgKills { get; set; }
    public int PistolKills { get; set; }
    public int GrenadeKills { get; set; }
    public int OtherKills { get; set; }
}