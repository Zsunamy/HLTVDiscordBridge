using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class PlayerIndividualStatistics
    {
        public PlayerIndividualStatistics(JObject jObject)
        {
            roundsWithKills = jObject.TryGetValue("roundsWithKills", out JToken roundsWithKillsTok) ? int.Parse(roundsWithKillsTok.ToString()) : int.MinValue;
            zeroKillRounds = jObject.TryGetValue("zeroKillRounds", out JToken zeroKillRoundsTok) ? int.Parse(zeroKillRoundsTok.ToString()) : int.MinValue;
            oneKillRounds = jObject.TryGetValue("oneKillRounds", out JToken oneKillRoundsTok) ? int.Parse(oneKillRoundsTok.ToString()) : int.MinValue;
            twoKillRounds = jObject.TryGetValue("twoKillRounds", out JToken twoKillRoundsTok) ? int.Parse(twoKillRoundsTok.ToString()) : int.MinValue;
            threeKillRounds = jObject.TryGetValue("threeKillRounds", out JToken threeKillRoundsTok) ? int.Parse(threeKillRoundsTok.ToString()) : int.MinValue;
            fourKillRounds = jObject.TryGetValue("fourKillRounds", out JToken fourKillRoundsTok) ? int.Parse(fourKillRoundsTok.ToString()) : int.MinValue;
            fiveKillRounds = jObject.TryGetValue("fiveKillRounds", out JToken fiveKillRoundsTok) ? int.Parse(fiveKillRoundsTok.ToString()) : int.MinValue;
            openingKills = jObject.TryGetValue("openingKills", out JToken openingKillsTok) ? int.Parse(openingKillsTok.ToString()) : int.MinValue;
            openingDeaths = jObject.TryGetValue("openingDeaths", out JToken openingDeathsTok) ? int.Parse(openingDeathsTok.ToString()) : int.MinValue;
            openingKillRatio = jObject.TryGetValue("openingKillRatio", out JToken openingKillRatioTok) ? float.Parse(openingKillRatioTok.ToString()) : float.MinValue;
            openingKillRating = jObject.TryGetValue("openingKillRating", out JToken openingKillRatingTok) ? float.Parse(openingKillRatingTok.ToString()) : float.MinValue;
            teamWinPercentAfterFirstKill = jObject.TryGetValue("teamWinPercentAfterFirstKill", out JToken teamWinPercentAfterFirstKillTok) ? float.Parse(teamWinPercentAfterFirstKillTok.ToString()) : float.MinValue;
            firstKillInWonRounds = jObject.TryGetValue("firstKillInWonRounds", out JToken firstKillInWonRoundsTok) ? float.Parse(firstKillInWonRoundsTok.ToString()) : float.MinValue;
            rifleKills = jObject.TryGetValue("rifleKills", out JToken rifleKillsTok) ? int.Parse(rifleKillsTok.ToString()) : int.MinValue;
            sniperKills = jObject.TryGetValue("sniperKills", out JToken sniperKillsTok) ? int.Parse(sniperKillsTok.ToString()) : int.MinValue;
            smgKills = jObject.TryGetValue("smgKills", out JToken smgKillsTok) ? int.Parse(smgKillsTok.ToString()) : int.MinValue;
            pistolKills = jObject.TryGetValue("pistolKills", out JToken pistolKillsTok) ? int.Parse(pistolKillsTok.ToString()) : int.MinValue;
            grenadeKills = jObject.TryGetValue("grenadeKills", out JToken grenadeKillsTok) ? int.Parse(grenadeKillsTok.ToString()) : int.MinValue;
            otherKills = jObject.TryGetValue("otherKills", out JToken otherKillsTok) ? int.Parse(otherKillsTok.ToString()) : int.MinValue;
        }

        public int roundsWithKills { get; set; }
        public int zeroKillRounds { get; set; }
        public int oneKillRounds { get; set; }
        public int twoKillRounds { get; set; }
        public int threeKillRounds { get; set; }
        public int fourKillRounds { get; set; }
        public int fiveKillRounds { get; set; }
        public int openingKills { get; set; }
        public int openingDeaths { get; set; }
        public float openingKillRatio { get; set; }
        public float openingKillRating { get; set; }
        public float teamWinPercentAfterFirstKill { get; set; }
        public float firstKillInWonRounds { get; set; }
        public int rifleKills { get; set; }
        public int sniperKills { get; set; }
        public int smgKills { get; set; }
        public int pistolKills { get; set; }
        public int grenadeKills { get; set; }
        public int otherKills { get; set; }
    }
}
