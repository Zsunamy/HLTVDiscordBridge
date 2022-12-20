using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Modules;

public static class HltvResults
{
    private const string Path = "./cache/results/results.json";

    private static async Task<List<Result>> GetLatestResults()
    {
        string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddDays(-2));
        string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
        ResultRequest request = new (startDate, endDate);
        return await request.SendRequest<List<Result>>("getResults");
    }

    private static async Task<List<(Result, Match)>> GetNewResults()
    {
        if (!await AutomatedMessageHelper.VerifyFile(Path, GetLatestResults))
        {
            return new List<(Result, Match)>();
        }

        List<Result> latestResults = await GetLatestResults();
        List<Result> oldResults = AutomatedMessageHelper.ParseFromFile<Result>(Path);
        List<(Result, Match)> matchResults = new();
        foreach (Result latestResult in latestResults)
        {
            bool found = oldResults.Any(oldResult => latestResult.Id == oldResult.Id);
            if (!found)
            {
                GetMatch request = new(latestResult.Id);
                matchResults.Add((latestResult, await request.SendRequest<Match>("getMatch")));
            }
        }

        return matchResults;
    }

    public static async Task SendNewResults()
    {
        Stopwatch watch = new(); watch.Start();
        foreach ((Result result, Match match) in await GetNewResults())
        {
            await Tools.SendMessagesWithWebhook(x => x.ResultWebhookId != null,
                x => x.ResultWebhookId, x=> x.ResultWebhookToken , GetResultEmbed(result, match), GetMessageComponent(match));
        }
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched results ({watch.ElapsedMilliseconds}ms)");
    }

    public static async Task<List<Result>> GetMatchResultsOfEvent(uint eventId)
    {
        List<uint> eventIds = new() { eventId };
        return await GetMatchResultsOfEvent(eventIds);
    }
    private static async Task<List<Result>> GetMatchResultsOfEvent(IEnumerable<uint> eventIds)
    {
        List<string> eventIdsString = eventIds.Select(eventId => eventId.ToString()).ToList();
        List<List<string>> values = new() { eventIdsString };
        List<string> properties = new() { "eventIds" };
        JArray req = await Tools.RequestApiJArray("getResults", properties, values);

        return req.Select(matchResult => new Result(matchResult as JObject)).ToList();
    }
    public static async Task<List<Result>> GetMatchResults(uint teamId)
    {
        List<string> teamIds = new() { teamId.ToString() };

        List<List<string>> values = new() { teamIds };
        List<string> properties = new() { "teamIds" };

        JArray req = await Tools.RequestApiJArray("getResults", properties, values);

        return req.Select(result => new Result(JObject.Parse(result.ToString()))).ToList();
    }
    private static Embed GetResultEmbed(Result result, Match match)
    {
        EmbedBuilder builder = new();
        string title = match.winnerTeam.name == match.team1.name ? $"👑 {match.team1.name} vs. {match.team2.name}" :
            $"{match.team1.name} vs. {match.team2.name} 👑";
        builder.WithTitle(title)
            .WithColor(Color.Red)
            .AddField("event:", $"[{match.eventObj.name}]({match.eventObj.link})\n{match.significance}")
            .AddField("winner:", $"[{match.winnerTeam.name}]({match.winnerTeam.link})", true)
            .AddField("format:", $"{GetFormatFromAcronym(match.format.type)} ({match.format.location})", true)
            .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", match.link)
            .WithCurrentTimestamp();
        string footerString = "";
        Emoji emo = new("⭐");
        for (int i = 1; i <= result.stars; i++)
        {
            footerString += emo;
        }
        builder.WithFooter(footerString);

        string mapsString = "";
        foreach(Map map in match.maps)
        {
            if(map.mapResult != null)
            {
                string mapHalfResultString = 
                    map.mapResult.mapHalfResults.Aggregate("", (current, mapHalfResult) => current + (current == "" ? $"{mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}" : $" | {mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}"));
                mapsString += $"{GetMapNameByAcronym(map.name)} ({map.mapResult.team1TotalRounds}:{map.mapResult.team2TotalRounds}) ({mapHalfResultString})\n";
            }
            else
            {
                mapsString += $"~~{GetMapNameByAcronym(map.name)}~~\n";
            }
        }
        builder.AddField("maps:", mapsString);
            
        if (match.highlights.Count != 0)
        {
            Highlight[] highlights = new Highlight[2];
            match.highlights.CopyTo(0, highlights, 0, 2);
            string highlightsString = highlights.Aggregate
                ("", (current, highlight) => current + $"[{SpliceText(highlight.title, 35)}]({highlight.link})\n\n");
            builder.AddField("highlights:", highlightsString);
        }

        return builder.Build();
    }
    private static MessageComponent GetMessageComponent(Match match)
    {
        ComponentBuilder compBuilder = new();
        compBuilder.WithButton("match statistics",
            match.format.type == "bo1" ? "overallstats_bo1" : "overallstats_def");
        return compBuilder.Build();
    }
    private static string GetFormatFromAcronym(string arg)
    {
        return arg switch
        {
            "bo1" => "Best of 1",
            "bo3" => "Best of 3",
            "bo5" => "Best of 5",
            "bo7" => "Best of 7",
            _ => "n.A",
        };
    }
    private static string GetMapNameByAcronym(string arg)
    {
        return arg switch
        {
            "tba" => "to be announced",
            "de_train" => "Train",
            "de_cbble" => "Cobble",
            "de_inferno" => "Inferno",
            "de_cache" => "Cache",
            "de_mirage" => "Mirage",
            "de_overpass" => "Overpass",
            "de_dust2" => "Dust 2",
            "de_nuke" => "Nuke",
            "de_tuscan" => "Tuscan",
            "de_vertigo" => "Vertigo",
            "de_season" => "Season",
            "de_ancient" => "Ancient",
            "de_anubis" => "Anubis",
            _ => arg[0].ToString().ToUpper() + arg[1..],
        };
    }
    private static string SpliceText(string text, int lineLength)
    {
        int charCount = 0;
        IEnumerable<string> lines = text.Split(new [] { " " }, StringSplitOptions.RemoveEmptyEntries)
            .GroupBy(w => (charCount += w.Length + 1) / lineLength)
            .Select(g => string.Join(" ", g));

        return string.Join("\n", lines.ToArray());
    }
}