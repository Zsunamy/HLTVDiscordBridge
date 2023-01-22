using System;
using System.Collections.Generic;
using System.Linq;
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
    public int? NumberOfTeams { get; set; }
    public bool? AllMatchesListed { get; set; }
    public string Link { get; set; }
    public EventTeam[] Teams { get; set; }
    public Prize[] PrizeDistribution { get; set; } = Array.Empty<Prize>();
    public Event[] RelatedEvents { get; set; }
    public EventFormat[] Formats { get; set; }
    public string[] MapPool { get; set; }
    public EventHighlight[] Highlights { get; set; } 
    public Article[] News { get; set; }
        
    public Embed ToStartedEmbed()
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"{Name} just started!")
            .AddField("startDate:", Tools.UnixTimeToDateTime(DateStart).ToShortDateString(), true)
            .AddField("endDate:", Tools.UnixTimeToDateTime(DateEnd).ToShortDateString(), true)
            .AddField("\u200b", "\u200b", true)
            .AddField("prize pool:", PrizePool, true)
            .AddField("location:", Location.Name, true)
            .AddField("\u200b", "\u200b", true);
        
        List<string> teams = new();
        foreach (EventTeam team in Teams)
        {
            if (string.Join("\n", teams).Length > 600)
            {
                teams.Add($"and {Teams.Length - Array.IndexOf(Teams, team)} more");
                break;
            }
            teams.Add($"[{team.Name}]({team.Link})");
        }
        if(!teams.Any())
            builder.AddField("teams:", teams);
        
        return builder.WithColor(Color.Gold)
            .WithThumbnailUrl(Logo)
            .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link)
            .WithCurrentTimestamp()
            .Build();
    }

    public Embed ToPastEmbed()
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"{Name} just ended!")
            .AddField("startDate:", Tools.UnixTimeToDateTime(DateStart).ToShortDateString(), true)
            .AddField("endDate:", Tools.UnixTimeToDateTime(DateEnd).ToShortDateString(), true)
            .AddField("\u200b", "\u200b", true)
            .AddField("prize pool:", PrizePool, true)
            .AddField("location:", Location.Name, true)
            .AddField("\u200b", "\u200b", true);
        
        Console.WriteLine(Id);
        List<string> prizeList = new();
        foreach (Prize prize in PrizeDistribution)
        {
            if(string.Join("\n", prizeList).Length > 600)
            {
                prizeList.Add($"and {PrizeDistribution.Length - Array.IndexOf(PrizeDistribution, prize) - 1} more");
                break;
            }
            List<string> prizes = new();
            if(prize.PrizePrize != null)
                prizes.Add($"wins: {prize.PrizePrize}");
            
            if(prize.QualifiesFor != null)
                prizes.Add($"qualifies for: [{prize.QualifiesFor.Name}]({prize.QualifiesFor.Link})");
            
            if(prize.OtherPrize != null)
                prizes.Add($"qualifies for: {prize.OtherPrize}");
            if (prize.Team != null)
                prizeList.Add($"{prize.Place} [{prize.Team.Name}]({prize.Team.Link}) {string.Join(" & ", prizes)}");
        }
        if(string.Join("\n", prizeList).Length > 0)
            builder.AddField("results:", string.Join("\n", prizeList));

        return builder.WithColor(Color.Gold)
            .WithThumbnailUrl(Logo)
            .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link)
            .WithCurrentTimestamp()
            .Build();
    }
        
    public Embed ToFullEmbed(Result[] results)
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
        string start = startDate > DateTime.Now ? "starting" : "started";
        string end = endDate > DateTime.Now ? "ending" : "ended";
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
                teams.Add($"and {Teams.Length - Array.IndexOf(Teams, team)} more");
                break;
            }
            teams.Add($"[{team.Name}]({team.Link})");
        }
        if (teams.Count != 0)
            builder.AddField("teams:", string.Join("\n", teams));
        
        if (startDate < DateTime.Now && endDate > DateTime.Now)
        {
            // live
            List<string> matchResultString = new();

            foreach (Result result in results)
            {
                if (string.Join("\n", matchResultString).Length > 700)
                {
                    matchResultString.Add($"and {results.Length - Array.IndexOf(results, result) - 1} more");
                    break;
                }
                matchResultString.Add($"[{result.Team1.Name} vs. {result.Team2.Name}]({result.Link})");
            }

            builder.AddField("latest results:", string.Join("\n", matchResultString), true);
        }
        else if (startDate < DateTime.Now && endDate < DateTime.Now)
        {
            // past
            List<string> prizeList = new();
            foreach (Prize prize in PrizeDistribution)
            {
                if (string.Join("\n", prizeList).Length > 600)
                {
                    prizeList.Add(
                        $"and {PrizeDistribution.Length - Array.IndexOf(PrizeDistribution, prize) - 1} more");
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
                    prizeList.Add(
                        $"{prize.Place} [{prize.Team.Name}]({prize.Team.Link}) {string.Join(" & ", prizes)}");
                }
            }

            if (prizeList.Count > 0)
            {
                builder.AddField("results:", string.Join("\n", prizeList));
            }
        }

        return builder.Build();
    }
}