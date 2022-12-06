using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord.Net;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Modules
{
    public class Tools
    {
        public static EmbedFooterBuilder GetRandomFooter ()
        {
            EmbedFooterBuilder builder = new();
            string[] footerStrings = File.ReadAllText("./cache/footer.txt").Split("\n");
            Random random = new();
            string footerString = footerStrings[random.Next(0, footerStrings.Length)];
            builder.Text = footerString;
            return builder;
        }
        public static async Task<JObject> RequestApiJObject(string endpoint, List<string> properties, List<string> values)
        {
            HttpClient http = new();

            Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{endpoint}");

            StringBuilder sb = new();
            StringWriter sw = new(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                await writer.WriteStartObjectAsync();
                await writer.WritePropertyNameAsync("delayBetweenPageRequests");
                await writer.WriteValueAsync(300);
                for(int i = 0; i < properties.Count; i++)
                {
                    await writer.WritePropertyNameAsync(properties[i]);
                    await writer.WriteValueAsync(values[i]);
                }
                await writer.WriteEndObjectAsync();
            }
            
            HttpResponseMessage resp = await http.PostAsync(uri, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
            string res = await resp.Content.ReadAsStringAsync();
            if(resp.IsSuccessStatusCode)
            {
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JObject.Parse(res);
            }
            try
            { 
                JObject error = JObject.Parse(await resp.Content.ReadAsStringAsync());
                throw new HltvApiException(error);
            }
            catch(JsonReaderException) { throw new Exception("Deployment Error"); }
        }
        public static async Task<JArray> RequestApiJArray(string endpoint, List<string> properties, List<string> values)
        {
            Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{endpoint}");

            StringBuilder sb = new();
            StringWriter sw = new (sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                await writer.WriteStartObjectAsync();
                await writer.WritePropertyNameAsync("delayBetweenPageRequests");
                await writer.WriteValueAsync(300);
                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        await writer.WritePropertyNameAsync(properties[i]);
                        await writer.WriteValueAsync(values[i]);
                    }
                }
                await writer.WriteEndObjectAsync();
            }

            HttpResponseMessage resp = await Program.GetInstance().DefaultHttpClient.PostAsync(uri, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
            string res = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JArray.Parse(res);
            }
            
            try
            {
                JObject error = JObject.Parse(await resp.Content.ReadAsStringAsync());
                throw new HltvApiException(error); 
            }
            catch (JsonReaderException) { throw new Exception("Deployment Error"); }
        }
        public static async Task<JArray> RequestApiJArray(string endpoint, List<string> properties, List<List<string>> values)
        {
            Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{endpoint}");

            StringBuilder sb = new();
            StringWriter sw = new (sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                await writer.WriteStartObjectAsync();
                await writer.WritePropertyNameAsync("delayBetweenPageRequests");
                await writer.WriteValueAsync(300);
                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        await writer.WritePropertyNameAsync(properties[i]);
                        await writer.WriteStartArrayAsync();
                        foreach(string s in values[i])
                            await writer.WriteValueAsync(s);
                        await writer.WriteEndArrayAsync();
                    }
                }
                await writer.WriteEndObjectAsync();
            }

            HttpResponseMessage resp = await Program.GetInstance().DefaultHttpClient.PostAsync(uri, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
            string res = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JArray.Parse(res);
            }
            try
            {
                JObject error = JObject.Parse(await resp.Content.ReadAsStringAsync());
                throw new HltvApiException(error);
            }
            catch (JsonReaderException) { throw new Exception("Deployment Error"); }
        }

        public static string GetHltvTimeFormat(DateTime date)
        {
            string day = date.Day.ToString();
            string month = date.Month.ToString();
            if (day.Length == 1)
            {
                day = $"0{day}";
            }
            
            if (month.Length == 1)
            {
                month = $"0{month}";
            }

            return $"{date.Year.ToString()}-{month}-{day}";
        }

        public static int GetIdFromUrl(string url)
        {
            return int.Parse(url.Split('/')[^2]);
        }

        public static Task SendMessagesWithWebhook(Expression<Func<ServerConfig, bool>> filter, Expression<Func<ServerConfig, ulong?>> getId,
            Expression<Func<ServerConfig, string>> getToken, Embed embed, MessageComponent component = null)
        {
            List<Webhook> webhooks = Config.GetCollection().FindSync(filter).ToList().Select(config =>
                new Webhook(getId.Compile()(config), getToken.Compile()(config))).ToList();
            
            List<Task> status = webhooks.Select(webhook => Task.Run(() =>
                {
                    DiscordWebhookClient webhookClient;
                    try
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        webhookClient = new DiscordWebhookClient((ulong)webhook.Id, webhook.Token);
                    }
                    catch (Exception e)
                    {
                        if (e is InvalidOperationException)
                        {
                            UpdateDefinition<ServerConfig> update = Builders<ServerConfig>.Update.Set(getId, null)
                                .Set(getToken, "");
                            return Config.GetCollection().UpdateOneAsync(filter, update);
                        }
                        throw;
                    }
                    return webhookClient.SendMessageAsync(embeds: new[] { embed }, components: component);
                })).ToList();
            StatsUpdater.StatsTracker.MessagesSent += webhooks.Count;
            StatsUpdater.UpdateStats();
            return Task.WhenAll(status);
        }

        public static bool CheckIfWebhookIsUsed(Webhook webhook, ServerConfig config)
        {
            return new[] { config.ResultWebhookId, config.NewsWebhookId, config.EventWebhookId }
                .GroupBy(x => x).Any(g => g.Count() > 1 && g.Key == webhook.Id);
        }

        public static async Task<Webhook?> CheckChannelForWebhook(SocketTextChannel channel, ServerConfig config)
        {
            Webhook[] webhooks = { new (config.ResultWebhookId, config.ResultWebhookToken),
                new (config.NewsWebhookId, config.NewsWebhookToken), new (config.EventWebhookId, config.EventWebhookToken) };
            foreach (RestWebhook webhook in await channel.GetWebhooksAsync())
            {
                Webhook channelWebhook = new(webhook.Id, webhook.Token);
                if (webhooks.Aggregate(false, (b, currentWebhook) =>
                        (currentWebhook.Id == channelWebhook.Id && currentWebhook.Token == channelWebhook.Token) || b))
                {
                    return channelWebhook;
                }
            }
            return null;
        }

        public static async Task DeleteWebhook(Webhook webhook)
        {
            if (webhook.Id != null)
            {
                try
                {
                    DiscordWebhookClient client = new((ulong)webhook.Id, webhook.Token);
                    await client.DeleteWebhookAsync();
                }
                catch (HttpException) {}
                catch (InvalidOperationException) {}
            }
        }
    }
}
