namespace HLTVDiscordBridge.Shared;

public class OngoingEventPreview
{
    public int Id { get; set; }
    public string Name { get; set; }
    public long DateStart { get; set; }
    public long DateEnd { get; set; }
    public bool Featured { get; set; }
}