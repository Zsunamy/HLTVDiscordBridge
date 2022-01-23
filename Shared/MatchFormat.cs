using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MatchFormat
    {
        public MatchFormat(JObject jObject)
        {
            type = jObject.TryGetValue("type", out JToken typeTok) ? typeTok.ToString() : null;
            location = jObject.TryGetValue("location", out JToken locationTok) ? locationTok.ToString() : null;
        }

        public string type { get; set; }
        public string location { get; set; }
    }
}
