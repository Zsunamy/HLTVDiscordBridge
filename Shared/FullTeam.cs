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
            Id = jObject.TryGetValue("id", out JToken idTok) ? int.Parse(idTok.ToString()) : 0;
            Name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            Logo = jObject.TryGetValue("logo", out JToken logoTok) ? logoTok.ToString() : null;
            Twitter = jObject.TryGetValue("twitter", out JToken twitterTok) ? twitterTok.ToString() : null;
            Country = jObject.TryGetValue("country", out JToken countryTok) ? new Country(countryTok as JObject) : null;
            rank = jObject.TryGetValue("rank", out JToken rankTok) ? int.Parse(rankTok.ToString()) : 0;
            List<TeamPlayer> players = new();
            if(jObject.TryGetValue("players", out JToken playersTok))
            {
                foreach (JToken player in playersTok)
                {
                    players.Add(new TeamPlayer(player as JObject));
                }
            }
            this.Players = players;
            List<uint> rankingDevelopment = new();
            if (jObject.TryGetValue("rankingDevelopment", out JToken rankingDevelopmentsTok))
            {
                foreach(JToken rankingDevelopmentTok in rankingDevelopmentsTok)
                {
                    rankingDevelopment.Add(uint.Parse(rankingDevelopmentTok.ToString()));
                }
            }
            RankingDevelopment = rankingDevelopment;
            List<News> news = new();
            if (jObject.TryGetValue("news", out JToken newsTok))
            {
                foreach (JToken newTok in newsTok)
                {
                    news.Add(new News(newTok as JObject));
                }
            }
            News = news;
            Link = Id != 0 && Name != null ? $"https://www.hltv.org/team/{Id}/{Name.Replace(' ', '-')}" : null;
            LocalThumbnailPath = jObject.TryGetValue("localThumbnailPath", out JToken localThumbnailPathTok) ? localThumbnailPathTok.ToString() : null;
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public string Twitter { get; set; }
        public Country Country { get; set; }
        public int rank { get; set; }
        public List<TeamPlayer> Players { get; set; }
        public List<uint> RankingDevelopment { get; set; }
        public List<News> News { get; set; }
        public string LocalThumbnailPath { get; set; }
        public string Link { get; set; }
    }
}
