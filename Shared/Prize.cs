using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class Prize
    {
        public Prize(JObject jObject)
        {
            place = jObject.TryGetValue("place", out JToken placeTok) ? placeTok.ToString() : null;
            prize = jObject.TryGetValue("prize", out JToken prizeTok) ? prizeTok.ToString() : null;
            otherPrize = jObject.TryGetValue("otherPrize", out JToken otherPrizeTok) ? otherPrizeTok.ToString() : null;
            qualifiesFor = jObject.TryGetValue("qualifiesFor", out JToken qualifiesForTok) ? new Event(qualifiesForTok as JObject) : null;
            team = jObject.TryGetValue("team", out JToken teamTok) ? new Team(teamTok as JObject) : null;

        }

        public string place { get; set; }
        public string prize { get; set; }
        public string otherPrize { get; set; }
        public Event qualifiesFor { get; set; }
        public Team team { get; set; }
    }
}
