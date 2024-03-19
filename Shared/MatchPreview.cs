using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class MatchPreview 
{
    public int Id { get; set; }
    public long Date { get; set; }
    public int Stars { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public string Format { get; set; }
    public Event Event { get; set; }
    public bool Live { get; set; }
    
    [JsonIgnore]
    public string Link => Team1 != null && Team2 != null ? $"https://www.hltv.org/matches/{Id}/" +
                                                           $"{Team1.Name.ToLower().Replace(" ", "-")}-vs-{Team2.Name.ToLower().Replace(" ", "-")}-" +
                                                           $"{Event.Name.ToLower().Replace(" ", "-")}" : null;
}