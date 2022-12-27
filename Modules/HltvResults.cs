using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvResults
{
    private const string Path = "./cache/results/results.json";

    private static async Task<List<Result>> GetLatestResults()
    {
        string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddDays(-2));
        string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
        GetResults request = new (startDate, endDate);
        return await request.SendRequest<List<Result>>();
    }

    private static async Task<List<Result>> GetNewResults()
    {
        if (!await AutomatedMessageHelper.VerifyFile(Path, GetLatestResults))
        {
            return new List<Result>();
        }

        List<Result> latestResults = await GetLatestResults();
        List<Result> oldResults = Tools.ParseFromFile<List<Result>>(Path);

        return (from latestResult in latestResults
            let found = oldResults.Any(oldResult => latestResult.Id == oldResult.Id)
            where !found select latestResult).ToList();
    }

    public static async Task SendNewResults()
    {
        Stopwatch watch = new(); watch.Start();
        foreach (Result result in await GetNewResults())
        {
            (Embed embed, MessageComponent component) = await result.ToEmbedAndComponent();
            await Tools.SendMessagesWithWebhook(x => x.ResultWebhookId != null,
                x => x.ResultWebhookId, x=> x.ResultWebhookToken, embed, component);
        }
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched results ({watch.ElapsedMilliseconds}ms)");
    }

    public static async Task<List<Result>> GetMatchResultsOfEvent(int eventId)
    {
        return await GetMatchResultsOfEvent(new List<int> {eventId});
    }
    private static async Task<List<Result>> GetMatchResultsOfEvent(List<int> eventIds)
    {
        GetResults request = new(eventIds: eventIds);
        return await request.SendRequest<List<Result>>();
    }
    public static async Task<List<Result>> GetMatchResults(int teamId)
    {
        List<int> teamIds = new() { teamId };
        GetResults request = new (teamIds: teamIds);
        return await request.SendRequest<List<Result>>();
    }
}