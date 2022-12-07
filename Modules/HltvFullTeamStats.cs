using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;

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
            JObject jObject;
            try
            {
                jObject = await Tools.RequestApiJObject("getTeamStats", properties, values);
            }
            catch(HltvApiExceptionLegacy) { throw; }
            return new FullTeamStats(jObject);
        }
    }
}
