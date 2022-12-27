using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Modules;

namespace HLTVDiscordBridge.Shared;

public class FullEvent
{
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
        builder.AddField("location:", Location.Name, true);
        builder.AddField("\u200b", "\u200b", true);
        List<string> teams = new();
        foreach (EventTeam team in Teams)
        {
            if (string.Join("\n", teams).Length > 600)
            {
                teams.Add($"and {Teams.Count - Teams.IndexOf(team)} more");
                break;
            }
            teams.Add($"[{team.Name}]({team.Link})");
        }
        if(teams.Count > 0)
            builder.AddField("teams:", string.Join("\n", teams));
        builder.WithColor(Color.Gold);
        builder.WithThumbnailUrl(Logo);
        builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link);
        builder.WithCurrentTimestamp();
        return builder.Build();
    }

    public Embed ToPastEmbed()
    {
        EmbedBuilder builder = new();
        builder.WithTitle($"{Name} just ended!");
        builder.AddField("startDate:", Tools.UnixTimeToDateTime(DateStart).ToShortDateString(), true);
        builder.AddField("endDate:", Tools.UnixTimeToDateTime(DateEnd).ToShortDateString(), true);
        builder.AddField("\u200b", "\u200b", true);
        builder.AddField("prize pool:", PrizePool, true);
        builder.AddField("location:", Location.Name, true);
        builder.AddField("\u200b", "\u200b", true);
        
        List<string> prizeList = new();
        foreach (Prize prize in PrizeDistribution)
        {
            if(string.Join("\n", prizeList).Length > 600)
            {
                prizeList.Add($"and {PrizeDistribution.Count - PrizeDistribution.IndexOf(prize)} more");
                break;
            }
            List<string> prizes = new();
            if(prize.PrizePrize != null)
                prizes.Add($"wins: {prize.PrizePrize}"); 
            if(prize.QualifiesFor != null)
                prizes.Add($"qualifies for: [{prize.QualifiesFor.Name}]({prize.QualifiesFor.Link})"); 
            if(prize.OtherPrize != null)
                prizes.Add($"qualifies for: {prize.OtherPrize}");

            prizeList.Add($"{prize.Place} [{prize.Team.Name}]({prize.Team.Link}) {string.Join(" & ", prizes)}");
        }
        if(prizeList.Count > 0)
        {
            builder.AddField("results:", string.Join("\n", prizeList));
        }
            
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
            .AddField("location:", Location.Name, true)
            .AddField("\u200b", "\u200b", true);

        List<string> teams = new();
        foreach (EventTeam team in Teams)
        {
            if (string.Join("\n", teams).Length > 600)
            {
                teams.Add($"and {Teams.Count - Teams.IndexOf(team)} more");
                break;
            }
            teams.Add($"[{team.Name}]({team.Link})");
        }
        if (teams.Count > 0)
            builder.AddField("teams:", string.Join("\n", teams));

        if (startDate > DateTime.Now && endDate > DateTime.Now)
        {
            //upcoming                
        } 
        else if(startDate < DateTime.Now && endDate > DateTime.Now)
        {
            List<Result> results = (await HltvResults.GetMatchResultsOfEvent(Id)).ToList();
            List<string> matchResultString = new();

            foreach (Result result in results)
            {
                if (string.Join("\n", matchResultString).Length > 700)
                {
                    matchResultString.Add($"and {results.Count - results.IndexOf(result)} more");
                    break;
                }
                matchResultString.Add($"[{result.Team1.Name} vs. {result.Team2.Name}]({result.Link})");
            }
            builder.AddField("latest results:", string.Join("\n", matchResultString), true);
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
                    prizes.Add($"wins: {prize.PrizePrize}");
                    if (prize.QualifiesFor != null)
                        prizes.Add($"qualifies for: [{prize.QualifiesFor.Name}]({prize.QualifiesFor.Link})");
                    if (prize.OtherPrize != null)
                        prizes.Add($"qualifies for: {prize.OtherPrize}");
                    prizeList.Add($"{prize.Place} [{prize.Team.Name}]({prize.Team.Link}) {string.Join(" & ", prizes)}");
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