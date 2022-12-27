using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class Prize
{
    public string Place { get; set; }
    [JsonPropertyName("prize")]
    public string PrizePrize { get; set; }
    public string OtherPrize { get; set; }
    public Event QualifiesFor { get; set; }
    public Team Team { get; set; }
}