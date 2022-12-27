using System.Collections.Generic;

namespace HLTVDiscordBridge.Shared;

public class TeamMembership
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public ulong StartDate { get; set; }
    public ulong LeaveDate { get; set; }
    public List<TeamTrophy> TeamTrophies { get; set; }
}