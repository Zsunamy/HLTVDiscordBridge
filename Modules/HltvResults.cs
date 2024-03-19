using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Notifications;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvResults
{
    private const string Path = "./cache/results.json";

    private static async Task<Result[]> GetLatestResults()
    {
        string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddDays(-2));
        string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
        GetResults request = new GetResults{StartDate = startDate, EndDate = endDate};
        return await request.SendRequest<Result[]>();
    }

    private static async Task<IEnumerable<Result>> GetNewResults()
    {
        if (!await Tools.VerifyFile(Path, GetLatestResults))
            return new List<Result>();

        Result[] latestResults = await GetLatestResults();
        Result[] oldResults = Tools.ParseFromFile<Result[]>(Path);
        Tools.SaveToFile(Path, latestResults);

        return latestResults.Where(newR => Array.Find(oldResults, oldR => newR.Id == oldR.Id) == null);
    }

    public static async Task SendNewResults()
    {
        Stopwatch watch = new(); watch.Start();
        foreach (Result result in await GetNewResults())
        {
            GetMatch request = new GetMatch{Id = result.Id};
            (Embed embed, MessageComponent component) = result.ToEmbedAndComponent(await request.SendRequest<Match>());
            await ResultNotifier.Instance.NotifyAll(embed, component);
        }

        await Program.Log(new LogMessage(LogSeverity.Verbose, nameof(HltvResults),
            $"fetched results ({watch.ElapsedMilliseconds}ms)"));
    }
}