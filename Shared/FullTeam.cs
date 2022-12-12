using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.HttpResponses;

namespace HLTVDiscordBridge.Shared
{
    public class FullTeam
    {
        public FullTeam(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            logo = jObject.TryGetValue("logo", out JToken logoTok) ? logoTok.ToString() : null;
            twitter = jObject.TryGetValue("twitter", out JToken twitterTok) ? twitterTok.ToString() : null;
            country = jObject.TryGetValue("country", out JToken countryTok) ? new Country(countryTok as JObject) : null;
            rank = jObject.TryGetValue("rank", out JToken rankTok) ? uint.Parse(rankTok.ToString()) : 0;
            List<TeamPlayer> players = new();
            if(jObject.TryGetValue("players", out JToken playersTok))
            {
                foreach (JToken player in playersTok)
                {
                    players.Add(new TeamPlayer(player as JObject));
                }
            }
            this.players = players;
            List<uint> rankingDevelopment = new();
            if (jObject.TryGetValue("rankingDevelopment", out JToken rankingDevelopmentsTok))
            {
                foreach(JToken rankingDevelopmentTok in rankingDevelopmentsTok)
                {
                    rankingDevelopment.Add(uint.Parse(rankingDevelopmentTok.ToString()));
                }
            }
            this.rankingDevelopment = rankingDevelopment;
            List<News> news = new();
            if (jObject.TryGetValue("news", out JToken newsTok))
            {
                foreach (JToken newTok in newsTok)
                {
                    news.Add(new News(newTok as JObject));
                }
            }
            this.news = news;
            link = id != 0 && name != null ? $"https://www.hltv.org/team/{id}/{name.Replace(' ', '-')}" : null;
            localThumbnailPath = jObject.TryGetValue("localThumbnailPath", out JToken localThumbnailPathTok) ? localThumbnailPathTok.ToString() : null;
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        public uint id { get; set; }
        public string name { get; set; }
        public string logo { get; set; }
        public string twitter { get; set; }
        public Country country { get; set; }
        public uint rank { get; set; }
        public List<TeamPlayer> players { get; set; }
        public List<uint> rankingDevelopment { get; set; }
        public List<News> news { get; set; }
        public string localThumbnailPath { get; set; }
        public string link { get; set; }
    }
}
