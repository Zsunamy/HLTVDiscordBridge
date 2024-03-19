using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class TeamPlayer
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public string TimeOnTeam { get; set; }
    public uint MapsPlayed { get; set; }
    public string Type { get; set; }
    
    [JsonIgnore]
    public string Link => $"https://www.hltv.org/player/{Id}/{Name.ToLower().Replace(" ", "-")}";
}