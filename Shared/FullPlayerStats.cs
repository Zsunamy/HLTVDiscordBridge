using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class FullPlayerStats
    {
        public FullPlayerStats() { }
        public FullPlayerStats(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            ign = jObject.TryGetValue("ign", out JToken ignTok) ? ignTok.ToString() : null;
            if(jObject.TryGetValue("image", out JToken imageTok))
            {
                string imageLink = imageTok.ToString();               
                if(!imageLink.Contains("http"))
                {
                    image = "https://www.hltv.org" + imageLink;
                }
                else { image = imageLink; }
            }
            age = jObject.TryGetValue("age", out JToken ageTok) ? ageTok.ToString() : null;
            country = jObject.TryGetValue("country", out JToken countryTok) ? new Country(countryTok as JObject) : null;
            team = jObject.TryGetValue("team", out JToken teamTok) ? new Team(teamTok as JObject) : null;
            overviewStatistics = jObject.TryGetValue("overviewStatistics", out JToken playerOverviewStatisticsTok) ? new PlayerOverviewStatistics(playerOverviewStatisticsTok as JObject) : null;
            individualStatistics = jObject.TryGetValue("individualStatistics", out JToken playerIndividualStatisticsTok) ? new PlayerIndividualStatistics(playerIndividualStatisticsTok as JObject) : null;
        }

        public uint id { get; set; }
        public string name { get; set; }
        public string ign { get; set; }
        public string image { get; set; }
        public string age { get; set; }
        public Country country { get; set; }
        public Team team { get; set; }
        public PlayerOverviewStatistics overviewStatistics { get; set; }
        public PlayerIndividualStatistics individualStatistics { get; set; }
    }
}
