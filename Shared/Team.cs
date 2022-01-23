using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class Team
    {
        public Team(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? idTok.ToString() : null;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            link = (id != null && name != null) ? $"https://www.hltv.org/team/{id}/{name.Replace(' ', '-')}" : null;
        }

        public string id { get; set; }
        public string name { get; set; }
        public string link { get; set; }
    }
}
