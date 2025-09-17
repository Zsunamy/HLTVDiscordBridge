namespace HLTVDiscordBridge.Shared;

public class MapMatchStatsOverview
{
    public TeamComparison FirstKills { get; set; }
    public TeamComparison ClutchesWon { get; set; }
    public PlayerStat MostKills { get; set; }
    public PlayerStat MostDamage { get; set; }
    public PlayerStat MostAssists { get; set; }
    public PlayerStat MostAwpKills { get; set; }
    public PlayerStat MostFirstKills { get; set; }
}