﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MatchMapStatsPlayer
    {
        public MatchMapStatsPlayer(JObject jObject)
        {
            player = jObject.TryGetValue("player", out JToken playerTok) ? new Player(playerTok as JObject) : null;
            kills = jObject.TryGetValue("kills", out JToken killsTok) ? int.Parse(killsTok.ToString()) : int.MinValue;
            hsKills = jObject.TryGetValue("hsKills", out JToken hsKillsTok) ? int.Parse(hsKillsTok.ToString()) : int.MinValue;
            assists = jObject.TryGetValue("assists", out JToken assistsTok) ? int.Parse(assistsTok.ToString()) : int.MinValue;
            flashAssists = jObject.TryGetValue("flashAssists", out JToken flashAssistsTok) ? int.Parse(flashAssistsTok.ToString()) : int.MinValue;
            deaths = jObject.TryGetValue("deaths", out JToken deathsTok) ? int.Parse(deathsTok.ToString()) : int.MinValue;
            KAST = jObject.TryGetValue("KAST", out JToken kastTok) ? float.Parse(kastTok.ToString()) : float.MinValue;
            killDeathsDifference = jObject.TryGetValue("killDeathsDifference", out JToken killDeathsDifferenceTok) ? int.Parse(killDeathsDifferenceTok.ToString()) : int.MinValue;
            ADR = jObject.TryGetValue("ADR", out JToken adrTok) ? float.Parse(adrTok.ToString()) : float.MinValue;
            firstKillsDifference = jObject.TryGetValue("firstKillsDifference", out JToken firstKillsDifferenceTok) ? int.Parse(firstKillsDifferenceTok.ToString()) : int.MinValue;
            rating1 = jObject.TryGetValue("rating1", out JToken rating1Tok) ? float.Parse(rating1Tok.ToString()) : float.MinValue;
        }
        public Player player { get; set; }
        public int kills { get; set; }
        public int hsKills { get; set; }
        public int assists { get; set; }
        public int flashAssists { get; set; }
        public int deaths { get; set; }
        public float KAST { get; set; }
        public int killDeathsDifference { get; set; }
        public float ADR { get; set; }
        public int firstKillsDifference { get; set; }
        public float rating1 { get; set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }
    }
}