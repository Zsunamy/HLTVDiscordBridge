using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules
{
    public static class HltvMatchMapStats
    {
        public static async Task<MatchMapStats> GetMatchMapStats(Map map)
        {
            GetMatchMapStats request = new GetMatchMapStats{Id = map.StatsId};
            return await request.SendRequest<MatchMapStats>();
        }
    }
}
