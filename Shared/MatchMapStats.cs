using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MatchMapStats
    {
        public MatchMapStats(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken statsIdTok) ? uint.Parse(statsIdTok.ToString()) : 0;
            matchId = jObject.TryGetValue("matchId", out JToken matchIdTok) ? uint.Parse(matchIdTok.ToString()) : 0;
            result = jObject.TryGetValue("result", out JToken resultTok) ? new MapResult(resultTok as JObject) : null;
            map = jObject.TryGetValue("map", out JToken mapTok) ? mapTok.ToString() : null;
            date = jObject.TryGetValue("date", out JToken dateTok) ? ulong.Parse(dateTok.ToString()) : 0;
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? new Team(team1Tok as JObject) : null;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? new Team(team2Tok as JObject) : null;
            eventObj = jObject.TryGetValue("event", out JToken eventTok) ? new Event(eventTok as JObject) : null;
            if (jObject.TryGetValue("playerStats", out JToken overviewTok))
            {
                List<MatchMapStatsPlayer> matchMapStatsPlayer1 = new();
                List<MatchMapStatsPlayer> matchMapStatsPlayer2 = new();
                if ((overviewTok as JObject).TryGetValue("team1", out JToken team1StatsTok))
                {
                    
                    foreach (JToken team1StatTok in team1StatsTok)
                    {
                        matchMapStatsPlayer1.Add(new MatchMapStatsPlayer(team1StatTok as JObject));
                    }
                    this.team1stats = matchMapStatsPlayer1;
                }
                if ((overviewTok as JObject).TryGetValue("team2", out JToken team2StatsTok))
                {
                    foreach (JToken team2StatTok in team2StatsTok)
                    {
                        matchMapStatsPlayer2.Add(new MatchMapStatsPlayer(team2StatTok as JObject));
                    }
                    this.team2stats = matchMapStatsPlayer2;
                }
            }
            link = $"https://www.hltv.org/stats/matches/mapstatsid/{id}/{team1.name.ToLower().Replace(' ', '-')}-vs-{team2.name.ToLower().Replace(' ', '-')}";
        }

        public uint id { get; set; }
        public uint matchId { get; set; }
        public MapResult result { get; set; }
        public string map { get; set; }
        public ulong date { get; set; }
        public Team team1 { get; set; }
        public Team team2 { get; set; }
        public Event eventObj { get; set; }
        public List<MatchMapStatsPlayer> team1stats { get; set; }
        public List<MatchMapStatsPlayer> team2stats { get; set; }
        public string link { get; set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }
    }
}
