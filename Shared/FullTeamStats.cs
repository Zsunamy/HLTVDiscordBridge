using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HLTVDiscordBridge.Shared
{
    public class FullTeamStats
    {
        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }
        public FullTeamStats(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            overview = jObject.TryGetValue("overview", out JToken overviewTok) ? new TeamOverviewStatistics(overviewTok as JObject) : null;
            List<TeamMapResult> matches = new();
            if(jObject.TryGetValue("matches", out JToken matchesTok))
            {
                foreach(JToken matchTok in matchesTok)
                {
                    matches.Add(new TeamMapResult(matchTok as JObject));
                }
            }
            this.matches = matches;
            List<Player> currentLineup = new();
            if (jObject.TryGetValue("currentLineup", out JToken currentLineupTok))
            {
                foreach (JToken playerTok in currentLineupTok)
                {
                    currentLineup.Add(new Player(playerTok as JObject));
                }
            }
            this.currentLineup = currentLineup;
            List<Player> historicPlayers = new();
            if (jObject.TryGetValue("historicPlayers", out JToken historicPlayersTok))
            {
                foreach (JToken playerTok in historicPlayersTok)
                {
                    historicPlayers.Add(new Player(playerTok as JObject));
                }
            }
            this.historicPlayers = historicPlayers;
            List<Player> standins = new();
            if (jObject.TryGetValue("standins", out JToken standinsTok))
            {
                foreach (JToken playerTok in standinsTok)
                {
                    standins.Add(new Player(playerTok as JObject));
                }
            }
            this.standins = standins;
            List<TeamEvent> events = new();
            if (jObject.TryGetValue("events", out JToken eventsTok))
            {
                foreach (JToken eventTok in eventsTok)
                {
                    events.Add(new TeamEvent(eventTok as JObject));
                }
            }
            this.events = events;
            mapStats = jObject.TryGetValue("mapStats", out JToken mapsStatsTok) ? new TeamMapsStats(mapsStatsTok as JObject) : null;
        }

        public uint id { get; set; }
        public string name { get; set; }
        public TeamOverviewStatistics overview { get; set; }
        public List<TeamMapResult> matches { get; set; }
        public List<Player> currentLineup { get; set; }
        public List<Player> historicPlayers { get; set; }
        public List<Player> standins { get; set; }
        public List<TeamEvent> events { get; set; }
        public TeamMapsStats mapStats { get; set; }
    }
}
