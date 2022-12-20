using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.HttpResponses;

namespace HLTVDiscordBridge.Shared
{
    public class TeamMapResult
    {
        public TeamMapResult(JObject jObject)
        {
            date = jObject.TryGetValue("date", out JToken dateTok) ? ulong.Parse(dateTok.ToString()) : 0;
            if(jObject.TryGetValue("event", out JToken eventTok))
            {
                eventObj = new Event(eventTok as JObject);
            } else if(jObject.TryGetValue("eventObj", out JToken eventObjTok))
            {
                eventObj = new Event(eventObjTok as JObject);
            }
            else
            {
                eventObj = null;
            }
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? new Team(team1Tok as JObject) : null;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? new Team(team2Tok as JObject) : null;
            map = jObject.TryGetValue("map", out JToken mapTok) ? mapTok.ToString() : null;
            mapStatsId = jObject.TryGetValue("mapStatsId", out JToken mapStatsIdTok) ? uint.Parse(mapStatsIdTok.ToString()) : 0;
            ResultLegacy = jObject.TryGetValue("result", out JToken resultTok) ? new Result_legacy(resultTok as JObject) : null;
        }

        public ulong date { get; set; }
        public Event eventObj { get; set; }
        public Team team1 { get; set; }
        public Team team2 { get; set; }
        public string map { get; set; }
        public uint mapStatsId { get; set; }
        public Result_legacy ResultLegacy { get; set; }
    }
}
