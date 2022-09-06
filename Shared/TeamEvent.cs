using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class TeamEvent
    {
        public TeamEvent(JObject jObject)
        {
            place = jObject.TryGetValue("place", out JToken placeTok) ? placeTok.ToString() : null;
            if(jObject.TryGetValue("event", out JToken eventTok))
            {
                eventObj = new Event(eventTok as JObject);
            } else if(jObject.TryGetValue("eventObj", out JToken eventObjTok))
            {
                eventObj = new Event(eventObjTok as JObject);
            } else
            {
                eventObj = null;
            }
        }

        public string place { get; set; }
        public Event eventObj { get; set; }
    }
}
