using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvNews
{
    private const string Path = "./cache/news.json";
    private static async Task<IEnumerable<RssNews>> GetNewNews()
    {
        if (!await AutomatedMessageHelper.VerifyFile(Path, GetLatestNews))
        {
            return Array.Empty<RssNews>();
        }
        RssNews[] latestNews = await GetLatestNews();
        RssNews[] oldNews = Tools.ParseFromFile<RssNews[]>(Path);
        Tools.SaveToFile(Path, latestNews);
        return from newItem in latestNews 
            where oldNews.All(oldItem => Tools.GetIdFromUrl(newItem.Link) != Tools.GetIdFromUrl(oldItem.Link))
            select newItem;
    }
    private static async Task<RssNews[]> GetLatestNews()
    {
        GetRssNews request = new();
        return await request.SendRequest<RssNews[]>();
    }
    public static async Task SendNewNews()
    {
        Stopwatch watch = new(); watch.Start();
        foreach (RssNews news in await GetNewNews())
        {
            await Tools.SendMessagesWithWebhook(x => x.News != null, x => x.News , news.ToEmbed());
        }
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched news ({watch.ElapsedMilliseconds}ms)");
    }
}