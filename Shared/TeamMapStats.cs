using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class TeamMapStats
    {
        public TeamMapStats(JObject jObject)
        {
            wins = getUIntFromJToken(jObject, nameof(wins));
            draws = getUIntFromJToken(jObject, nameof(draws));
            losses = getUIntFromJToken(jObject, nameof(losses));
            winRate = getFloatFromJToken(jObject, nameof(winRate));
            totalRounds = getUIntFromJToken(jObject, nameof(totalRounds));
            roundWinPAfterFirstKill = getFloatFromJToken(jObject, nameof(roundWinPAfterFirstKill));
            roundWinPAfterFirstDeath = getFloatFromJToken(jObject, nameof(roundWinPAfterFirstDeath));
        }

        public uint wins { get; set; }
        public uint draws { get; set; }
        public uint losses { get; set; }
        public float winRate { get; set; }
        public uint totalRounds { get; set; }
        public float roundWinPAfterFirstKill { get; set; }
        public float roundWinPAfterFirstDeath { get; set; }

        private uint getUIntFromJToken(JObject jObject, string tokenName)
        {
            if(jObject == null) return 0;
            return jObject.TryGetValue(tokenName, out JToken jToken) ? uint.Parse(jToken.ToString()) : 0;
        }
        private float getFloatFromJToken(JObject jObject, string tokenName)
        {
            if(jObject == null) return 0.0f;
            return jObject.TryGetValue(tokenName, out JToken jToken) ? float.Parse(jToken.ToString()) : float.MinValue;
        }
    }
}
