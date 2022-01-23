using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class EventFormat
    {
        public EventFormat(JObject jObject)
        {
            type = jObject.TryGetValue("type", out JToken typeTok) ? typeTok.ToString() : null;
            description = jObject.TryGetValue("description", out JToken descriptionTok) ? descriptionTok.ToString() : null;
        }

        public string type { get; set; }
        public string description { get; set; }
    }
}
