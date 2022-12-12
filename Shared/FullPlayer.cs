using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.HttpResponses;

namespace HLTVDiscordBridge.Shared
{
    public class FullPlayer
    {
        public FullPlayer() { }
        public FullPlayer(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            ign = jObject.TryGetValue("ign", out JToken ignTok) ? ignTok.ToString() : null;
            if (jObject.TryGetValue("image", out JToken imageTok))
            {
                string imageLink = imageTok.ToString();
                if (!imageLink.Contains("http"))
                {
                    image = "https://www.hltv.org" + imageLink;
                }
                else { image = imageLink; }
            }
            age = jObject.TryGetValue("age", out JToken ageTok) ? ageTok.ToString() : null;
            twitter = jObject.TryGetValue("twitter", out JToken twitterTok) ? twitterTok.ToString() : null;
            twitch = jObject.TryGetValue("twitch", out JToken twitchTok) ? twitchTok.ToString() : null;
            instagram = jObject.TryGetValue("instagram", out JToken instagramTok) ? instagramTok.ToString() : null;
            country = jObject.TryGetValue("country", out JToken countryTok) ? new Country(countryTok as JObject) : null;
            team = jObject.TryGetValue("team", out JToken teamTok) ? new Team(teamTok as JObject) : null;
            List<Achievement> achievements = new();
            if(jObject.TryGetValue("achievements", out JToken achievementsTok))
            {
                foreach (JToken achievementTok in achievementsTok)
                {
                    achievements.Add(new Achievement(achievementTok as JObject));
                }
                this.achievements = achievements;
            }
            List<TeamMembership> teamMemberships = new();
            if (jObject.TryGetValue("teams", out JToken teamMembershipsTok))
            {
                foreach (JToken teamMembershipTok in teamMembershipsTok)
                {
                    teamMemberships.Add(new TeamMembership(teamMembershipTok as JObject));
                }
                this.teamMemberships = teamMemberships;
            }
            List<News> news = new();
            if (jObject.TryGetValue("news", out JToken newsTok))
            {
                foreach (JToken newTok in newsTok)
                {
                    news.Add(new News(newTok as JObject));
                }
                this.news = news;
            }

        }

        public uint id { get; set; }
        public string name { get; set; }
        public string ign { get; set; }
        public string image { get; set; }
        public string age { get; set; }
        public string twitter { get; set; }
        public string twitch { get; set; }
        public string instagram { get; set; }
        public Country country { get; set; }
        public Team team { get; set; }
        public List<Achievement> achievements { get; set; }
        public List<TeamMembership> teamMemberships { get; set; }
        public List<News> news { get; set; }
    }
}
