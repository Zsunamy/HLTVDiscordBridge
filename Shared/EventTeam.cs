using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class EventTeam
    {
        public EventTeam(JObject jObject)
        {
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            reasonForParticipation = jObject.TryGetValue("reasonForParticipation", out JToken reasonForParticipationTok) ? reasonForParticipationTok.ToString() : null;
            rankDuringEvent = jObject.TryGetValue("rankDuringEvent", out JToken rankDuringEventTok) ? ushort.Parse(rankDuringEventTok.ToString()) : (ushort)0;
            link = id != 0 && name != null ? $"https://www.hltv.org/team/{id}/{name.ToLower().Replace(' ', '-')}" : null;
        }

        public string name { get; set; }
        public uint id { get; set; }
        public string reasonForParticipation { get; set; }
        public ushort rankDuringEvent { get; set; }
        public string link { get; set; }
    }
}
