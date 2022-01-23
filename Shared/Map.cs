using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class Map
    {
        public Map(JObject jObject)
        {
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            mapResult = jObject.TryGetValue("result", out JToken resultTok) ? new MapResult(JObject.Parse(resultTok.ToString())) : null;
        }

        public string name { get; set; }
        public MapResult mapResult { get; set; }
    }
}
