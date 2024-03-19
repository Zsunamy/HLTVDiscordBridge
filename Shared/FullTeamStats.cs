using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class FullTeamStats
{
    public int Id { get; set; }
    public string Name { get; set; }
    public TeamOverviewStatistics Overview { get; set; }
    public TeamMapResult[] Matches { get; set; }
    public Player[] CurrentLineup { get; set; }
    public Player[] HistoricPlayers { get; set; }
    public Player[] Standins { get; set; }
    public TeamEvent[] Events { get; set; }
    public TeamMapsStats MapStats { get; set; }
}