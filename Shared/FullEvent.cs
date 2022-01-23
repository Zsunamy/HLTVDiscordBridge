using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class FullEvent
    {
        public FullEvent(JObject jObject)
        {
            id = jObject.TryGetValue("id", out JToken idTok) ? uint.Parse(idTok.ToString()) : 0;
            name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            logo = jObject.TryGetValue("logo", out JToken logoTok) ? logoTok.ToString() : null;
            dateStart = jObject.TryGetValue("dateStart", out JToken dateStartTok) ? ulong.Parse(dateStartTok.ToString()) : 0;
            dateEnd = jObject.TryGetValue("dateEnd", out JToken dateEndTok) ? ulong.Parse(dateEndTok.ToString()) : 0;
            prizePool = jObject.TryGetValue("prizePool", out JToken prizePoolTok) ? prizePoolTok.ToString() : null;
            location = jObject.TryGetValue("location", out JToken locationTok) ? new Location(locationTok as JObject) : null;
            numberOfTeams = jObject.TryGetValue("numberOfTeams", out JToken numberOfTeamsTok) ? ushort.Parse(numberOfTeamsTok.ToString()) : (ushort)0;
            allMatchesListed = jObject.TryGetValue("allMatchesListed", out JToken allMatchesListedTok) ? bool.Parse(allMatchesListedTok.ToString()) : false;
            link = id != 0 && name != null ? $"https://www.hltv.org/events/{id}/{name.ToLower().Replace(' ', '-')}" : null;
            List<EventTeam> teams = new();
            if(jObject.TryGetValue("teams", out JToken teamsTok))
            {
                foreach(JToken teamTok in teamsTok)
                {
                    teams.Add(new EventTeam(teamTok as JObject));
                }
                this.teams = teams;
            }
            List<Prize> prizeDistribution = new();
            if(jObject.TryGetValue("prizeDistribution", out JToken prizeDistributionTok))
            {
                foreach(JToken prizeTok in prizeDistributionTok)
                {
                    prizeDistribution.Add(new Prize(prizeTok as JObject));
                }
                this.prizeDistribution = prizeDistribution;
            }
            List<Event> relatedEvents = new();
            if(jObject.TryGetValue("relatedEvents", out JToken relatedEventsTok))
            {
                foreach(JToken relatedEventTok in relatedEventsTok)
                {
                    relatedEvents.Add(new Event(relatedEventTok as JObject));
                }
                this.relatedEvents = relatedEvents;
            }
            List<EventFormat> formats = new();
            if(jObject.TryGetValue("formats", out JToken formatsTok))
            {
                foreach(JToken formatTok in formatsTok)
                {
                    formats.Add(new EventFormat(formatTok as JObject));
                }
                this.formats = formats;
            }
            List<string> mapPool = new();
            if(jObject.TryGetValue("mapPool", out JToken mapPoolTok))
            {
                foreach(JToken map in mapPoolTok)
                {
                    mapPool.Add(map.ToString());
                }
                this.mapPool = mapPool;
            }
            List<EventHighlight> highlights = new();
            if(jObject.TryGetValue("highlights", out JToken hightlightsTok))
            {
                foreach(JToken hightlightTok in hightlightsTok)
                {
                    highlights.Add(new EventHighlight(hightlightTok as JObject));
                }
                this.highlights = highlights;
            }
            List<News> news = new();
            if(jObject.TryGetValue("news", out JToken newsTok))
            {
                foreach(JToken newTok in newsTok)
                {
                    news.Add(new News(newTok as JObject));
                }
                this.news = news;
            }
        }
        public uint id { get; set; }
        public string name { get; set; }
        public string logo { get; set; }
        public ulong dateStart { get; set; }
        public ulong dateEnd { get; set; }
        public string prizePool { get; set; }
        public Location location { get; set; }
        public ushort numberOfTeams { get; set; }
        public bool allMatchesListed { get; set; }
        public string link { get; set; }
        public List<EventTeam> teams { get; set; }
        public List<Prize> prizeDistribution { get; set; }
        public List<Event> relatedEvents { get; set; }
        public List<EventFormat> formats { get; set; }
        public List<string> mapPool { get; set; }
        public List<EventHighlight> highlights { get; set; } 
        public List<News> news { get; set; }
    }
}
