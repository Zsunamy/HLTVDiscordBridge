namespace HLTVDiscordBridge.Shared;

public class EventHighlight
{
    public string Name { get; set; }
    public string Link { get; set; }
    public string Thumbnail { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public int? Views { get; set; }
}