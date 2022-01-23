using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class Player
    {
        public Player(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            link = id != 0 && name != null ? $"https://www.hltv.org/player/{id}/{name}" : null;
        }

        public uint id { get; set; }
        public string name { get; set; }
        public string link { get; set; }
    }
}
