using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class PlayerOverviewStatistics
    {
        public PlayerOverviewStatistics(JObject jObject)
        {
            kills = jObject.TryGetValue("kills", out JToken killsTok) ? int.Parse(killsTok.ToString()) : int.MinValue;
            headshots = jObject.TryGetValue("headshots", out JToken headshotsTok) ? float.Parse(headshotsTok.ToString()) : float.MinValue;
            deaths = jObject.TryGetValue("deaths", out JToken deathsTok) ? int.Parse(deathsTok.ToString()) : int.MinValue;
            kdRatio = jObject.TryGetValue("kdRatio", out JToken kdRatioTok) ? float.Parse(kdRatioTok.ToString()) : float.MinValue;
            damagePerRound = jObject.TryGetValue("damagePerRound", out JToken damagePerRoundTok) ? float.Parse(damagePerRoundTok.ToString()) : float.MinValue;
            grenadeDamagePerRound = jObject.TryGetValue("grenadeDamagePerRound", out JToken grenadeDamagePerRoundTok) ? float.Parse(grenadeDamagePerRoundTok.ToString()) : float.MinValue;
            mapsPlayed = jObject.TryGetValue("mapsPlayed", out JToken mapsPlayedTok) ? int.Parse(mapsPlayedTok.ToString()) : int.MinValue;
            roundsPlayed = jObject.TryGetValue("roundsPlayed", out JToken roundsPlayedTok) ? int.Parse(roundsPlayedTok.ToString()) : int.MinValue;
            killsPerRound = jObject.TryGetValue("killsPerRound", out JToken killsPerRoundTok) ? float.Parse(killsPerRoundTok.ToString()) : float.MinValue;
            assistsPerRound = jObject.TryGetValue("assistsPerRound", out JToken assistsPerRoundTok) ? float.Parse(assistsPerRoundTok.ToString()) : float.MinValue;
            deathsPerRound = jObject.TryGetValue("deathsPerRound", out JToken deathsPerRoundTok) ? float.Parse(deathsPerRoundTok.ToString()) : float.MinValue;
            savedByTeammatePerRound = jObject.TryGetValue("savedByTeammatePerRound", out JToken savedByTeammatePerRoundTok) ? float.Parse(savedByTeammatePerRoundTok.ToString()) : float.MinValue;
            savedTeammatesPerRound = jObject.TryGetValue("savedTeammatesPerRound", out JToken savedTeammatesPerRoundTok) ? float.Parse(savedTeammatesPerRoundTok.ToString()) : float.MinValue;
            rating2 = jObject.TryGetValue("rating2", out JToken rating2Tok) ? float.Parse(rating2Tok.ToString()) : float.MinValue;
        }

        public int kills { get; set; }
        public float headshots { get; set; }
        public int deaths { get; set; }
        public float kdRatio { get; set; }
        public float damagePerRound { get; set; }
        public float grenadeDamagePerRound { get; set; }
        public int mapsPlayed { get; set; }
        public int roundsPlayed { get; set; }
        public float killsPerRound { get; set; }
        public float assistsPerRound { get; set; }
        public float deathsPerRound { get; set; }
        public float savedByTeammatePerRound { get; set; }
        public float savedTeammatesPerRound { get; set; }
        public float rating2 { get; set; }
    }
}
