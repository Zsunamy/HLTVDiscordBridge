using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MatchUpcoming 
    {
        public MatchUpcoming(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            date = jObject.TryGetValue("date", out JToken dateTok) ? ulong.Parse(dateTok.ToString()) : 0;
            stars = jObject.TryGetValue("stars", out JToken starsTok) ? ushort.Parse(starsTok.ToString()) : ushort.MinValue;
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? new Team(JObject.Parse(team1Tok.ToString())) : null;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? new Team(JObject.Parse(team2Tok.ToString())) : null;
            format = jObject.TryGetValue("format", out JToken formatTok) ? formatTok.ToString() : null;
            if (jObject.TryGetValue("event", out JToken eventTok))
            {
                eventObj = new Event(eventTok as JObject);
            }
            else if (jObject.TryGetValue("eventObj", out JToken eventObjTok))
            {
                eventObj = new Event(eventObjTok as JObject);
            }
            else
            {
                eventObj = null;
            }
            live = jObject.TryGetValue("live", out JToken liveTok) ? bool.Parse(liveTok.ToString()) : false;
            link = (id != 0 && team1 != null && team2 != null) ? $"https://www.hltv.org/matches/{id}/{team1.name.Replace(' ', '-')}-vs-{team2.name.Replace(' ', '-')}" : null;
        }
        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        public uint id { get; set; }
        public ulong date { get; set; }
        public ushort stars { get; set; }
        public Team team1 { get; set; }
        public Team team2 { get; set; }
        public string format { get; set; }
        public Event eventObj { get; set; }
        public bool live { get; set; }
        public string link { get; set; }
    }
}
