using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class Match
    {
        public Match(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            statsId = jObject.TryGetValue("statsId", out JToken statsIdTok) ? uint.Parse(statsIdTok.ToString()) : 0;
            significance = jObject.TryGetValue("significance", out JToken significanceTok) ? significanceTok.ToString() : null;            
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? new Team(JObject.Parse(team1Tok.ToString())) : null;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? new Team(JObject.Parse(team2Tok.ToString())) : null;
            winnerTeam = jObject.TryGetValue("winnerTeam", out JToken winnerTeamTok) ? new Team(JObject.Parse(winnerTeamTok.ToString())) : null;
            date = jObject.TryGetValue("date", out JToken dateTok) ? ulong.Parse(dateTok.ToString()) : 0;
            format = jObject.TryGetValue("format", out JToken formatTok) ? new Format(JObject.Parse(formatTok.ToString())) : null;
            eventObj = jObject.TryGetValue("event", out JToken eventTok) ? new Event(JObject.Parse(eventTok.ToString())) : null;
            List<Map> maps = new();
            if(jObject.TryGetValue("maps", out JToken mapsTok))
            {
                foreach(JToken mapTok in mapsTok)
                {
                    maps.Add(new Map(JObject.Parse(mapTok.ToString())));
                }
                this.maps = maps;
            }
            List<Highlight> highlights = new();
            if (jObject.TryGetValue("highlights", out JToken highlightsTok))
            {
                foreach (JToken highlightTok in highlightsTok)
                {
                    highlights.Add(new Highlight(JObject.Parse(highlightTok.ToString())));
                }
                this.highlights = highlights;
            }
            link = (id != 0 && team1 != null && team2 != null) ? $"https://www.hltv.org/matches/{id}/{team1.name.Replace(' ', '-')}-vs-{team2.name.Replace(' ', '-')}" : null;
        }

        public uint id { get; set; }
        public uint statsId { get; set; }
        public string significance { get; set; }
        public Team team1 { get; set; }
        public Team team2 { get; set; }
        public Team winnerTeam { get; set; }
        public ulong date { get; set; }
        public Format format { get; set; }
        public Event eventObj { get; set; }
        public List<Map> maps { get; set; }
        public List<Highlight> highlights { get; set; }
        public string link { get; set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }
    }
}
