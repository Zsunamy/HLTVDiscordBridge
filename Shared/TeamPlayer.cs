using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class TeamPlayer
    {
        public TeamPlayer(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            timeOnTeam = jObject.TryGetValue("timeOnTeam", out JToken timeOnTeamTok) ? timeOnTeamTok.ToString() : null;
            mapsPlayed = jObject.TryGetValue("mapsPlayed", out JToken mapsPlayedTok) ? uint.Parse(mapsPlayedTok.ToString()) : 0;
            type = jObject.TryGetValue("type", out JToken typeTok) ? typeTok.ToString() : null;
            link = id != 0 && name != null ? $"https://www.hltv.org/player/{id}/{name.Replace(" ", "%20")}" : null;
        }

        public uint id { get; set; }
        public string name { get; set; }
        public string timeOnTeam { get; set; }
        public uint mapsPlayed { get; set; }
        public string type { get; set; }
        public string link { get; set; }
    }
}
