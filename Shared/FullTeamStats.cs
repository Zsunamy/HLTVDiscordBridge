using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class FullTeamStats
{
    public int Id { get; set; }
    public string Name { get; set; }
    public TeamOverviewStatistics Overview { get; set; }
    public List<TeamMapResult> Matches { get; set; }
    public List<Player> CurrentLineup { get; set; }
    public List<Player> HistoricPlayers { get; set; }
    public List<Player> Standins { get; set; }
    public List<TeamEvent> Events { get; set; }
    public TeamMapsStats MapStats { get; set; }
}