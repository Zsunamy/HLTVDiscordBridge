using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class EventHighlight
    {
        public EventHighlight(JObject jObject)
        {
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            link = jObject.TryGetValue("link", out JToken linkTok) ? linkTok.ToString() : null;
            thumbnail = jObject.TryGetValue("thumbnail", out JToken thumbnailTok) ? thumbnailTok.ToString() : null;
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? new Team(team1Tok as JObject) : null;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? new Team(team2Tok as JObject) : null;
            views = jObject.TryGetValue("views", out JToken viewsTok) ? uint.Parse(viewsTok.ToString()) : 0;
        }

        public string name { get; set; }
        public string link { get; set; }
        public string thumbnail { get; set; }
        public Team team1 { get; set; }
        public Team team2 { get; set; }
        public uint views { get; set; }
    }
}
