using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class TeamRanking
{
    public Team Team { get; set; }
    public int Points { get; set; }
    public int Place { get; set; }
    public int Change { get; set; }
    public bool IsNew { get; set; }
}