using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules
{
    public class HltvFullTeamStats
    {
        public static async Task<FullTeamStats> GetFullTeamStats(uint id)
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("id");
            values.Add(id.ToString());
            return new FullTeamStats((await Tools.RequestApiJObject("getTeamStats", properties, values)).Item1);
        }
    }
}
