using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class Team
{
    public string Name { get; set; }
    public int? Id { get; set; }
    [JsonIgnore]
    public string Link => Id != null && Name != null ? $"https://www.hltv.org/team/{Id}/{Name.Replace(' ', '-').ToLower()}" : null;
}