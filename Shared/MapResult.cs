using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MapResult
    {
        public MapResult(JObject jObject)
        {
            team1TotalRounds = jObject.TryGetValue("team1TotalRounds", out JToken team1TotalRoundsTok) ? team1TotalRoundsTok.ToString() : null;
            team2TotalRounds = jObject.TryGetValue("team2TotalRounds", out JToken team2TotalRoundsTok) ? team2TotalRoundsTok.ToString() : null;
            List<MapHalfResult> mapHalfResults = new();
            if(jObject.TryGetValue("halfResults", out JToken halfResults))
            {
                foreach(JToken halfResult in halfResults)
                {
                    mapHalfResults.Add(new MapHalfResult(JObject.Parse(halfResult.ToString())));
                }
                this.mapHalfResults = mapHalfResults;
            } else { this.mapHalfResults = null; }
        }

        public string team1TotalRounds { get; set; }
        public string team2TotalRounds { get; set; }
        public List<MapHalfResult> mapHalfResults { get; set; }
    }
}
