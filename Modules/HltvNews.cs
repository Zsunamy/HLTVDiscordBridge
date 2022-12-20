using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.HttpResponses;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvNews
{
    private const string Path = "./cache/news/news.json";
    private static async Task<List<News>> GetNewNews()
    {
        if (!await AutomatedMessageHelper.VerifyFile(Path, GetLatestNews))
        {
            return new List<News>();
        }
        List<News> latestNews = await GetLatestNews();
        List<News> oldNews = AutomatedMessageHelper.ParseFromFile<News>(Path);
        AutomatedMessageHelper.SaveToFile(Path, latestNews);
        return (from newItem in latestNews 
            where oldNews.All(oldItem => Tools.GetIdFromUrl(newItem.Link) != Tools.GetIdFromUrl(oldItem.Link))
            select newItem).ToList();
    }
    private static async Task<List<News>> GetLatestNews()
    {
        ApiRequestBody request = new();
        return await request.SendRequest<List<News>>("getRssNews");
    }
    public static async Task SendNewNews()
    {
        Stopwatch watch = new(); watch.Start();
        foreach (News news in await GetNewNews())
        {
            await Tools.SendMessagesWithWebhook(x => x.NewsWebhookId != null,
                x => x.NewsWebhookId, x=> x.NewsWebhookToken , news.ToEmbed());
        }
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched news ({watch.ElapsedMilliseconds}ms)");
    }
}