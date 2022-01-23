using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class TeamOverviewStatistics
    {
        public TeamOverviewStatistics(JObject jObject)
        {
            mapsPlayed = getUIntFromJToken(jObject, "mapsPlayed");
            totalKills = getUIntFromJToken(jObject, "totalKills");
            totalDeaths = getUIntFromJToken(jObject, "totalKills");
            roundsPlayed = getUIntFromJToken(jObject, "roundsPlayed");
            kdRatio = jObject.TryGetValue("kdRatio", out JToken kdRatioTok) ? float.Parse(kdRatioTok.ToString()) : float.MinValue;
            wins = getUIntFromJToken(jObject, "wins");
            draws = getUIntFromJToken(jObject, "draws");
            losses = getUIntFromJToken(jObject, "losses");
        }

        public uint mapsPlayed { get; set; }
        public uint totalKills { get; set; }
        public uint totalDeaths { get; set; }
        public uint roundsPlayed { get; set; }
        public float kdRatio { get; set; }
        public uint wins { get; set; }
        public uint draws { get; set; }
        public uint losses { get; set; }

        private uint getUIntFromJToken(JObject jObject, string tokenName)
        {
            return jObject.TryGetValue(tokenName, out JToken jToken) ? uint.Parse(jToken.ToString()) : 0;
        }
    }
}
