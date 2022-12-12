using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public class HltvNews
{
    private static async Task<bool> VerifyFile(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                JsonDocument.Parse(await File.ReadAllTextAsync(path));
                return true;
            }
            catch (JsonException) {}
        }
        News.SaveToFile(await GetLatestNews());
        return false;
    }
    private static async Task<List<News>> GetNewNews()
    {
        if (!await VerifyFile(News.Path))
        {
            return new List<News>();
        }
        List<News> latestNews = await GetLatestNews();
        List<News> oldNews = News.ParseFromFile();
        News.SaveToFile(latestNews);
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