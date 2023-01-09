using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Modules;

public static class Tools
{
    public static EmbedFooterBuilder GetRandomFooter()
    {
        EmbedFooterBuilder builder = new();
        string[] footerStrings = File.ReadAllText("./cache/footer.txt").Split("\n");
        Random random = new();
        string footerString = footerStrings[random.Next(0, footerStrings.Length)];
        builder.Text = footerString;
        return builder;
    }

    public static string GetHltvTimeFormat(DateTime date)
    {
        string day = date.Day.ToString();
        string month = date.Month.ToString();

        day = day.Length == 1 ? $"0{day}" : day;
        month = month.Length == 1 ? $"0{month}" : month;

        return $"{date.Year.ToString()}-{month}-{day}";
    }

    public static int GetIdFromUrl(string url)
    {
        return int.Parse(url.Split('/')[^2]);
    }

    public static Task SendMessagesWithWebhook(Expression<Func<ServerConfig, bool>> filter,
        Expression<Func<ServerConfig, Webhook>> getWebhook, Embed embed, MessageComponent component = null)
    {
        Webhook[] webhooks = Config.GetCollection().Find(filter).ToList().Select(config => getWebhook.Compile()(config)).ToArray();

        IEnumerable<Task> status = webhooks.Select(webhook => Task.Run(() =>
        {
            try
            {
                return webhook.ToDiscordWebhookClient().SendMessageAsync(embeds: new[] { embed }, components: component);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException or InvalidCastException)
                {
                    UpdateDefinition<ServerConfig> update = Builders<ServerConfig>.Update.Set(getWebhook, null);
                    return Config.GetCollection().UpdateOneAsync(filter, update);
                }

                StatsTracker.GetStats().MessagesSent -= 1;
                throw;
            }
        }));
        StatsTracker.GetStats().MessagesSent += webhooks.Length;
        return Task.WhenAll(status);
    }

    public static string GetFormatFromAcronym(string arg)
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

    public static string GetMapNameByAcronym(string arg)
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
            _ => arg[0].ToString().ToUpper() + arg[1..]
        };
    }

    public static string SpliceText(string text, int lineLength)
    {
        int charCount = 0;
        IEnumerable<string> lines = text.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
            .GroupBy(w => (charCount += w.Length + 1) / lineLength)
            .Select(g => string.Join(" ", g));

        return string.Join("\n", lines.ToArray());
    }
        
    public static DateTime UnixTimeToDateTime(long unixTimeStamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp).DateTime;
    }
    
    public static void SaveToFile(string path, object content)
    {
        if (!File.Exists(path))
            File.Create(path!).Dispose();
        
        File.WriteAllText(path, JsonSerializer.Serialize(content, Program.SerializeOptions));
    }
    
    public static T ParseFromFile<T>(string path)
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Program.SerializeOptions);
    }
    
    public static async Task<bool> VerifyFile<T>(string path, Func<Task<T>> getNewData)
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
        SaveToFile(path, await getNewData());
        return false;
    }
}