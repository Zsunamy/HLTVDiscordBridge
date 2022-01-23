using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Modules
{
    public class HltvMatch
    {
        public static async Task<Match> GetMatch(string url)
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("id");
            values.Add(url.Substring(29, 7));
            var req = await Tools.RequestApiJObject("getMatch", properties, values);
            if (!req.Item2 || req.Item1 == null) { return null; }
            Match match = new(req.Item1);
            return match;
        }
        public static async Task<Match> GetMatch(MatchResult matchResult)
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("id");
            values.Add(matchResult.id.ToString());
            var req = await Tools.RequestApiJObject("getMatch", properties, values);
            if (!req.Item2 || req.Item1 == null) { return null; }
            Match match = new(req.Item1);
            return match;
        }
    }
}
