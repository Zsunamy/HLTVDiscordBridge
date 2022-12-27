namespace HLTVDiscordBridge.Shared;

public class FullPlayerTeam : Team
{
    public long StartDate { get; set; }
    public long LeaveDate { get; set; }
    
    public Event[] Trophies { get; set; }
}