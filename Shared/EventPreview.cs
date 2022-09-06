using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class EventPreview
    {
        public EventPreview(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            dateStart = jObject.TryGetValue("dateStart", out JToken dateStartTok) ? ulong.Parse(dateStartTok.ToString()) : 0;
            dateEnd = jObject.TryGetValue("dateEnd", out JToken dateEndTok) ? ulong.Parse(dateEndTok.ToString()) : 0;
            location = jObject.TryGetValue("location", out JToken locationTok) ? new Location(locationTok as JObject) : null;
            prizePool = jObject.TryGetValue("prizePool", out JToken prizePoolTok) ? prizePoolTok.ToString() : null;
            numberOfTeams = jObject.TryGetValue("numberOfTeams", out JToken numberOfTeamsTok) ? uint.Parse(numberOfTeamsTok.ToString()) : 0;
            featured = jObject.TryGetValue("featured", out JToken featuredTok) ? bool.Parse(featuredTok.ToString()) : false;
        }

        public uint id { get; set; }
        public string name { get; set; }
        public ulong dateStart { get; set; }
        public ulong dateEnd { get; set; }
        public Location location { get; set; }
        public string prizePool { get; set; }
        public uint numberOfTeams { get; set; }
        public bool featured { get; set; }

        
    }
}
