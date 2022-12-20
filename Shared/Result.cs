using HLTVDiscordBridge.Modules;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.HttpResponses;

namespace HLTVDiscordBridge.Shared
{
    public class Result
    {
        public Result(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : uint.MinValue;
            stars = jObject.TryGetValue("stars", out JToken starsTok) ? ushort.Parse(starsTok.ToString()) : ushort.MinValue;
            date = jObject.TryGetValue("date", out JToken dateTok) ? ulong.Parse(dateTok.ToString()) : ulong.MinValue;
            team1 = jObject.TryGetValue("team1", out JToken team1Tok) ? new Team(team1Tok as JObject) : null;
            team2 = jObject.TryGetValue("team2", out JToken team2Tok) ? new Team(team2Tok as JObject) : null;
            ResultLegacy = jObject.TryGetValue("result", out JToken resultTok) ? new Result_legacy(resultTok as JObject) : null;
            format = jObject.TryGetValue("format", out JToken formatTok) ? formatTok.ToString() : null;
            link = (id != 0 && team1 != null && team2 != null) ? $"https://www.hltv.org/matches/{id}/{team1.name.Replace(' ', '-')}-vs-{team2.name.Replace(' ', '-')}" : null;
        }

        public uint id { get; set; }
        public int Id { get; set; }
        public ushort stars { get; set; }
        public ulong date { get; set; }
        public Team team1 { get; set; }
        public Team team2 { get; set; }
        public Result_legacy ResultLegacy { get; set; }
        public string format { get; set; }
        public string link { get; set; }
        
    }
}
