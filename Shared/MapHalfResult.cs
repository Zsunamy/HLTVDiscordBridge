using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MapHalfResult
    {
        public MapHalfResult(JObject jObject)
        {
            team1Rounds = jObject.TryGetValue("team1Rounds", out JToken team1RoundsTok) ? team1RoundsTok.ToString() : null;
            team2Rounds = jObject.TryGetValue("team2Rounds", out JToken team2RoundsTok) ? team2RoundsTok.ToString() : null;
        }

        public string team1Rounds { get; set; }
        public string team2Rounds { get; set; }
    }
}
