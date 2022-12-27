using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules
{
    public class HltvFullTeamStats
    {
        public static async Task<FullTeamStats> GetFullTeamStats(int id)
        {
            GetTeamStats request = new(id);
            return await request.SendRequest<FullTeamStats>();
        }
    }
}
