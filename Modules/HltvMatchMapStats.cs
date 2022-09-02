using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules
{
    public static class HltvMatchMapStats
    {
        public static async Task<MatchMapStats> GetMatchMapStats(Map map)
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("id");
            values.Add(map.statsId.ToString());
            var req = await Tools.RequestApiJObject("getMatchMapStats", properties, values);
            if (req == null) { return null; }
            MatchMapStats matchMapStats = new(req);
            return matchMapStats;
        }
    }
}
