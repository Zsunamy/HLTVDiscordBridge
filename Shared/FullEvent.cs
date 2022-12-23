using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using HLTVDiscordBridge.HttpResponses;

namespace HLTVDiscordBridge.Shared
{
    public class FullEvent
    {
        public FullEvent(JObject jObject)
        {
            Id = jObject.TryGetValue("id", out JToken idTok) ? int.Parse(idTok.ToString()) : 0;
            Name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            if (jObject.TryGetValue("logo", out JToken logoTok))
            {
                string logoLink = logoTok.ToString();
                if (!logoLink.Contains("http"))
                {
                    Logo = "https://www.hltv.org" + logoLink;
                }
                else { Logo = logoLink; }
            }
            DateStart = jObject.TryGetValue("dateStart", out JToken dateStartTok) ? ulong.Parse(dateStartTok.ToString()) : 0;
            DateEnd = jObject.TryGetValue("dateEnd", out JToken dateEndTok) ? ulong.Parse(dateEndTok.ToString()) : 0;
            PrizePool = jObject.TryGetValue("prizePool", out JToken prizePoolTok) ? prizePoolTok.ToString() : null;
            Location = jObject.TryGetValue("location", out JToken locationTok) ? new Location(locationTok as JObject) : null;
            NumberOfTeams = jObject.TryGetValue("numberOfTeams", out JToken numberOfTeamsTok) ? ushort.Parse(numberOfTeamsTok.ToString()) : (ushort)0;
            AllMatchesListed = jObject.TryGetValue("allMatchesListed", out JToken allMatchesListedTok) ? bool.Parse(allMatchesListedTok.ToString()) : false;
            Link = Id != 0 && Name != null ? $"https://www.hltv.org/events/{Id}/{Name.ToLower().Replace(' ', '-')}" : null;
            List<EventTeam> teams = new();
            if(jObject.TryGetValue("teams", out JToken teamsTok))
            {
                foreach(JToken teamTok in teamsTok)
                {
                    teams.Add(new EventTeam(teamTok as JObject));
                }
                this.Teams = teams;
            }
            List<Prize> prizeDistribution = new();
            if(jObject.TryGetValue("prizeDistribution", out JToken prizeDistributionTok))
            {
                foreach(JToken prizeTok in prizeDistributionTok)
                {
                    prizeDistribution.Add(new Prize(prizeTok as JObject));
                }
                this.PrizeDistribution = prizeDistribution;
            }
            List<Event> relatedEvents = new();
            if(jObject.TryGetValue("relatedEvents", out JToken relatedEventsTok))
            {
                foreach(JToken relatedEventTok in relatedEventsTok)
                {
                    relatedEvents.Add(new Event(relatedEventTok as JObject));
                }
                this.RelatedEvents = relatedEvents;
            }
            List<EventFormat> formats = new();
            if(jObject.TryGetValue("formats", out JToken formatsTok))
            {
                foreach(JToken formatTok in formatsTok)
                {
                    formats.Add(new EventFormat(formatTok as JObject));
                }
                this.Formats = formats;
            }
            List<string> mapPool = new();
            if(jObject.TryGetValue("mapPool", out JToken mapPoolTok))
            {
                foreach(JToken map in mapPoolTok)
                {
                    mapPool.Add(map.ToString());
                }
                this.MapPool = mapPool;
            }
            List<EventHighlight> highlights = new();
            if(jObject.TryGetValue("highlights", out JToken hightlightsTok))
            {
                foreach(JToken hightlightTok in hightlightsTok)
                {
                    highlights.Add(new EventHighlight(hightlightTok as JObject));
                }
                this.Highlights = highlights;
            }
            List<News> news = new();
            if(jObject.TryGetValue("news", out JToken newsTok))
            {
                foreach(JToken newTok in newsTok)
                {
                    news.Add(new News(newTok as JObject));
                }
                this.News = news;
            }
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public ulong DateStart { get; set; }
        public ulong DateEnd { get; set; }
        public string PrizePool { get; set; }
        public Location Location { get; set; }
        public int NumberOfTeams { get; set; }
        public bool AllMatchesListed { get; set; }
        public string Link { get; set; }
        public List<EventTeam> Teams { get; set; }
        public List<Prize> PrizeDistribution { get; set; }
        public List<Event> RelatedEvents { get; set; }
        public List<EventFormat> Formats { get; set; }
        public List<string> MapPool { get; set; }
        public List<EventHighlight> Highlights { get; set; } 
        public List<News> News { get; set; }
    }
}
