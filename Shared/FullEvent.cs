using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.HttpResponses;
using HLTVDiscordBridge.Modules;

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
            DateStart = jObject.TryGetValue("dateStart", out JToken dateStartTok) ? long.Parse(dateStartTok.ToString()) : 0;
            DateEnd = jObject.TryGetValue("dateEnd", out JToken dateEndTok) ? long.Parse(dateEndTok.ToString()) : 0;
            PrizePool = jObject.TryGetValue("prizePool", out JToken prizePoolTok) ? prizePoolTok.ToString() : null;
            Location = jObject.TryGetValue("location", out JToken locationTok) ? new Location(locationTok as JObject) : null;
            NumberOfTeams = jObject.TryGetValue("numberOfTeams", out JToken numberOfTeamsTok) ? ushort.Parse(numberOfTeamsTok.ToString()) : (ushort)0;
            AllMatchesListed = jObject.TryGetValue("allMatchesListed", out JToken allMatchesListedTok) && bool.Parse(allMatchesListedTok.ToString());
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
                mapPool.AddRange(mapPoolTok.Select(map => map.ToString()));
                MapPool = mapPool;
            }
            List<EventHighlight> highlights = new();
            if(jObject.TryGetValue("highlights", out JToken highlightsTok))
            {
                highlights.AddRange(highlightsTok.Select(highlightTok => new EventHighlight(highlightTok as JObject)));
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
        public long DateStart { get; set; }
        public long DateEnd { get; set; }
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
        
        public Embed ToStartedEmbed()
        {
            EmbedBuilder builder = new();
            builder.WithTitle($"{Name} just started!");
            builder.AddField("startDate:", Tools.UnixTimeToDateTime(DateStart).ToShortDateString(), true);
            builder.AddField("endDate:", Tools.UnixTimeToDateTime(DateEnd).ToShortDateString(), true);
            builder.AddField("\u200b", "\u200b", true);
            builder.AddField("prize pool:", PrizePool, true);
            builder.AddField("location:", Location.name, true);
            builder.AddField("\u200b", "\u200b", true);
            List<string> teams = new();
            foreach (EventTeam team in Teams)
            {
                if (string.Join("\n", teams).Length > 600)
                {
                    teams.Add($"and {Teams.Count - Teams.IndexOf(team)} more");
                    break;
                }
                teams.Add($"[{team.name}]({team.link})");
            }
            if(teams.Count > 0)
                builder.AddField("teams:", string.Join("\n", teams));
            builder.WithColor(Color.Gold);
            builder.WithThumbnailUrl(Logo);
            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link);
            builder.WithCurrentTimestamp();
            return builder.Build();
        }
        
        public async Task<Embed> ToFullEmbed()
    {
        EmbedBuilder builder = new();
        builder.WithTitle($"{Name}")
            .WithColor(Color.Gold)
            .WithThumbnailUrl(Logo)
            .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link)
            .WithFooter(Tools.GetRandomFooter())
            .WithCurrentTimestamp();
        DateTime startDate = Tools.UnixTimeToDateTime(DateStart);
        DateTime endDate = Tools.UnixTimeToDateTime(DateEnd);
        string start = startDate > DateTime.UtcNow ? "starting" : "started";
        string end = endDate > DateTime.UtcNow ? "ending" : "ended";
        builder.AddField(start, startDate.ToShortDateString(), true)
            .AddField(end, endDate.ToShortDateString(), true)
            .AddField("\u200b", "\u200b", true)
            .AddField("prize pool:", PrizePool, true)
            .AddField("location:", Location.name, true)
            .AddField("\u200b", "\u200b", true);

        List<string> teams = new();
        foreach (EventTeam team in Teams)
        {
            if (string.Join("\n", teams).Length > 600)
            {
                teams.Add($"and {Teams.Count - Teams.IndexOf(team)} more");
                break;
            }
            teams.Add($"[{team.name}]({team.link})");
        }
        if (teams.Count > 0)
            builder.AddField("teams:", string.Join("\n", teams));

        if (startDate > DateTime.UtcNow && endDate > DateTime.UtcNow)
        {
            //upcoming                
        } 
        else if(startDate < DateTime.UtcNow && endDate > DateTime.UtcNow)
        {
            List<Result> results = await HltvResults.GetMatchResultsOfEvent(Id);
            List<string> matchResultString = new();
            if (results.Count > 0)
            {
                    
                foreach (Result result in results)
                {
                    if (string.Join("\n", matchResultString).Length > 700)
                    {
                        matchResultString.Add($"and {results.Count - results.IndexOf(result)} more");
                        break;
                    }
                    matchResultString.Add($"[{result.Team1.name} vs. {result.Team2.name}]({result.Link})");
                }
                builder.AddField("latest results:", string.Join("\n", matchResultString), true);
            }                
            //live
        } 
        else
        {
            List<string> prizeList = new();
            foreach (Prize prize in PrizeDistribution)
            {
                if (string.Join("\n", prizeList).Length > 600)
                {
                    prizeList.Add($"and {PrizeDistribution.Count - PrizeDistribution.IndexOf(prize)} more");
                    break;
                }
                List<string> prizes = new();
                if (prize != null)
                {
                    prizes.Add($"wins: {prize.prize}");
                    if (prize.qualifiesFor != null)
                        prizes.Add($"qualifies for: [{prize.qualifiesFor.name}]({prize.qualifiesFor.link})");
                    if (prize.otherPrize != null)
                        prizes.Add($"qualifies for: {prize.otherPrize}");
                    prizeList.Add($"{prize.place} [{prize.team.name}]({prize.team.link}) {string.Join(" & ", prizes)}");
                }
            }
            if (prizeList.Count > 0)
            {
                builder.AddField("results:", string.Join("\n", prizeList));
            }
            //past
        }

        return builder.Build();
    }
    }
}
