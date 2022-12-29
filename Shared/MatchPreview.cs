namespace HLTVDiscordBridge.Shared;

public class MatchPreview 
{
    public uint Id { get; set; }
    public ulong Date { get; set; }
    public ushort Stars { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public string Format { get; set; }
    public Event EventObj { get; set; }
    public bool Live { get; set; }
    public string Link { get; set; }
}