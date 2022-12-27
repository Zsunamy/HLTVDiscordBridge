using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class Map
{
    public string Name { get; set; }
    public MapResult Result { get; set; }
    public int StatsId { get; set; }
}