namespace HLTVDiscordBridge.Shared;

public class EventPreview
{
    public int Id { get; set; }
    public string Name { get; set; }
    public long DateStart { get; set; }
    public long DateEnd { get; set; }
    public Location Location { get; set; }
    public string PrizePool { get; set; }
    public uint NumberOfTeams { get; set; }
    public bool Featured { get; set; }

        
}