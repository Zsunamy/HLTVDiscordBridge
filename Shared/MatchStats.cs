using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MatchStats
    {
        public MatchStats(JObject jObject)
        {
            statsId = jObject.TryGetValue("id", out JToken statsIdTok) ? uint.Parse(statsIdTok.ToString()) : 0;
            matchId = jObject.TryGetValue("matchId", out JToken matchIdTok) ? uint.Parse(matchIdTok.ToString()) : 0;
            if(jObject.TryGetValue("mapStatIds", out JToken mapStatIdsTok))
            {
                List<uint> mapStatIds = new();
                foreach(JToken mapStatIdTok in mapStatIdsTok)
                {
                    mapStatIds.Add(uint.Parse(mapStatIdTok.ToString()));
                }
                this.mapStatIds = mapStatIds;
            }
            date = jObject.TryGetValue("date", out JToken dateTok) ? ulong.Parse(dateTok.ToString()) : 0;
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? new Team(team1Tok as JObject) : null;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? new Team(team2Tok as JObject) : null;
            eventObj = jObject.TryGetValue("event", out JToken eventTok) ? new Event(eventTok as JObject) : null;
            matchStatsPlayerTeams = jObject.TryGetValue("playerStats", out JToken matchStatsPlayerTeamsTok) ? new MatchStatsPlayerTeams(matchStatsPlayerTeamsTok as JObject) : null;
            link = (statsId != 0 && team1 != null && team2 != null) ? $"https://www.hltv.org/stats/matches/{statsId}/{team1.name.Replace(' ', '-').ToLower()}-vs-{team2.name.Replace(' ', '-').ToLower()}" : null;
        }

        public uint statsId { get; set; }
        public uint matchId { get; set; }
        public List<uint> mapStatIds { get; set; }
        public ulong date { get; set; }
        public Team team1 { get; set; }
        public Team team2 { get; set; }
        public Event eventObj { get; set; }
        public MatchStatsPlayerTeams matchStatsPlayerTeams { get; set; }
        public string link { get; set; }
    }
}
