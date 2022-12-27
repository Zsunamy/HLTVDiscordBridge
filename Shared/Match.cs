using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class Match
{

    public int Id { get; set; }
    public int StatsId { get; set; }
    public string Significance { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public Team WinnerTeam { get; set; }
    public ulong Date { get; set; }
    public Format Format { get; set; }
    public Event EventObj { get; set; }
    public List<Map> Maps { get; set; }
    public List<Highlight> Highlights { get; set; }
    public string Link { get; set; }
}