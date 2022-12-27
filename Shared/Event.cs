using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class Event
{
    public int? Id { get; set; }
    public string Name { get; set; }
    [JsonIgnore]
    public string Link => Id != null && Name != null ? $"https://www.hltv.org/events/{Id}/{Name.Replace(' ', '-')}" : null;
}