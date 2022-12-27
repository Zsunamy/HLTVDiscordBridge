namespace HLTVDiscordBridge.Shared;

public class FullPlayerStats
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Ign { get; set; }
    public string Image { get; set; }
    public string Age { get; set; }
    public Country Country { get; set; }
    public Team Team { get; set; }
    public PlayerOverviewStatistics OverviewStatistics { get; set; }
    public PlayerIndividualStatistics IndividualStatistics { get; set; }
}