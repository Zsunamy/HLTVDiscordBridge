using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class TeamMembership
    {
        public TeamMembership(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            startDate = jObject.TryGetValue("startDate", out JToken startDateTok) ? ulong.Parse(startDateTok.ToString()) : 0;
            leaveDate = jObject.TryGetValue("leaveDate", out JToken leaveDateTok) ? ulong.Parse(leaveDateTok.ToString()) : 0;
            List<TeamTrophie> teamTrophies = new();
            if (jObject.TryGetValue("trophies", out JToken trophiesTok))
            {
                foreach(JToken trophieTok in trophiesTok)
                {
                    teamTrophies.Add(new TeamTrophie(trophieTok as JObject));
                }
                this.teamTrophies = teamTrophies;
            }
        }

        public uint id { get; set; }
        public string name { get; set; }
        public ulong startDate { get; set; }
        public ulong leaveDate { get; set; }
        public List<TeamTrophie> teamTrophies { get; set; }
    }
}
