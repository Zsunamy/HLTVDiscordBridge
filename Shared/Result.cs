using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Shared
{
    public class Result
    {
        public Result(JObject jObject)
        {
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? ushort.Parse(team1Tok.ToString()) : ushort.MinValue;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? ushort.Parse(team2Tok.ToString()) : ushort.MinValue;
        }

        public ushort team1 { get; set; }
        public ushort team2 { get; set; }
    }
}
