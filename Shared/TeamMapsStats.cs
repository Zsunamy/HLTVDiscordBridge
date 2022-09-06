using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class TeamMapsStats
    {
        public TeamMapsStats(JObject jObject)
        {
            de_dust2 = getTeamMapStats(jObject, nameof(de_dust2));
            de_mirage = getTeamMapStats(jObject, nameof(de_mirage));
            de_inferno = getTeamMapStats(jObject, nameof(de_inferno));
            de_nuke = getTeamMapStats(jObject, nameof (de_nuke));
            de_overpass = getTeamMapStats(jObject, nameof(de_overpass));
            de_train = getTeamMapStats(jObject, nameof(de_train));
            de_cache = getTeamMapStats(jObject, nameof(de_cache));
            de_cbble = getTeamMapStats(jObject, nameof(de_cbble));
            de_ancient = getTeamMapStats(jObject, nameof(de_ancient));
            de_tuscan = getTeamMapStats(jObject, nameof(de_tuscan));
        }

        public TeamMapStats de_dust2 { get; set; }
        public TeamMapStats de_mirage { get; set; }
        public TeamMapStats de_inferno { get; set; }
        public TeamMapStats de_nuke { get; set; }
        public TeamMapStats de_overpass { get; set; }
        public TeamMapStats de_train { get; set; }
        public TeamMapStats de_cache { get; set; }
        public TeamMapStats de_cbble { get; set; }
        public TeamMapStats de_ancient { get; set; }
        public TeamMapStats de_tuscan { get; set; }

        private TeamMapStats getTeamMapStats (JObject jObject, string tokenname)
        {
            return jObject.TryGetValue(tokenname, out JToken token) ? new TeamMapStats(token as JObject) : null;
        }
    }
}
