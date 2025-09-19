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
        GetResults request = new();
        return await request.SendRequest<Result[]>();
    }

    private static async Task<IEnumerable<Result>> GetNewResults()
    {
        if (!await Tools.VerifyFile(Path, async () => (await GetLatestResults()).ToDictionary(r => r.Id, r => r)))
            return [];

        Result[] latestResultsList = await GetLatestResults();
        Dictionary<int, Result> latestResults = latestResultsList.ToDictionary(r => r.Id, r => r);
        
        Dictionary<int, Result> oldResults = Tools.ParseFromFile<Dictionary<int, Result>>(Path);
        Tools.SaveToFile(Path, latestResults);

        return latestResults.Where(pair => !oldResults.ContainsKey(pair.Key)).Select(pair => pair.Value);
    }

    public static async Task SendNewResults()
    {
        Stopwatch watch = new(); watch.Start();
        IEnumerable<Result> newResults = await GetNewResults();
        int processedCount = 0;
        
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