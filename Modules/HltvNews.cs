using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HLTVDiscordBridge.HttpResponses;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvNews
{
    public const string Path = "./cache/news/news.json";
    public static List<News> ParseFromFile()
    {
        return JsonSerializer.Deserialize<List<News>>(File.ReadAllText(Path), ApiRequestBody.SerializeOptions);
    }
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
        SaveToFile(await GetLatestNews());
        return false;
    }

    private static void SaveToFile(object content)
    {
        File.WriteAllText(Path, JsonSerializer.Serialize(content, ApiRequestBody.SerializeOptions));
    }
    private static async Task<List<News>> GetNewNews()
    {
        if (!await VerifyFile(Path))
        {
            return new List<News>();
        }
        List<News> latestNews = await GetLatestNews();
        List<News> oldNews = ParseFromFile();
        SaveToFile(latestNews);
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