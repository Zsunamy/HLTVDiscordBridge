using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class Match
{
    public int Id { get; set; }
    public int StatsId { get; set; }
    public string Significance { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public Team WinnerTeam { get; set; }
    public long Date { get; set; }
    public Format Format { get; set; }
    public Event Event { get; set; }
    public Map[] Maps { get; set; }
    public Highlight[] Highlights { get; set; }
    [JsonIgnore]
    public string Link => Team1 != null && Team2 != null ? $"https://www.hltv.org/matches/{Id}/" +
                                                           $"{Team1.Name.ToLower().Replace(" ", "-")}-vs-{Team2.Name.ToLower().Replace(" ", "-")}-" +
                                                           $"{Event.Name.ToLower().Replace(" ", "-")}" : null;
}