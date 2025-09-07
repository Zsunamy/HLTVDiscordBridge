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
        var newResults = await GetNewResults();
        var processedCount = 0;
        
        foreach (Result result in newResults)
        {
            try
            {
                GetMatch request = new GetMatch{Id = result.Id};
                Match data = await request.SendRequest<Match>();
                (Embed embed, MessageComponent component) = result.ToEmbedAndComponent(data);
                await ResultNotifier.Instance.NotifyAll(result.Stars, embed, component);
                processedCount++;
                
                // Force GC every 10 results to prevent memory buildup
                if (processedCount % 10 == 0)
                {
                    GC.Collect(0, GCCollectionMode.Optimized);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new MyLogMessage(LogSeverity.Error, nameof(HltvResults), 
                    $"Failed to process result {result.Id}: {ex.Message}"));
            }
        }

        Logger.Log(new MyLogMessage(LogSeverity.Verbose, nameof(HltvResults),
            $"Processed {processedCount} results ({watch.ElapsedMilliseconds}ms)"));
    }
}