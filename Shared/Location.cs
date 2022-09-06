using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class Location
    {
        public Location(JObject jObject)
        {
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            code = jObject.TryGetValue("code", out JToken codeTok) ? codeTok.ToString() : null;
        }

        public string name { get; set; }
        public string code { get; set; }
    }
}
