using System;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HLTVDiscordBridge.Modules
{
    public class HltvNews : ModuleBase<SocketCommandContext>
    {
        //official RSS Feed       
        private static async Task<List<News>> GetNewNews()
        {
            List<News> latestNews = await GetLatestNews();
            JArray oldNewsJArray;
            // if (!File.Exists("./cache/news/news.json")) { var fs = File.Create("./cache/news/news.json");  fs.Close(); }

            try
            {
                oldNewsJArray = JArray.Parse(await File.ReadAllTextAsync("./cache/news/news.json"));
            }
            catch (JsonReaderException)
            {
                await File.WriteAllTextAsync("./cache/news/news.json", JArray.FromObject(latestNews).ToString());
                return new List<News>();
            }
            catch (FileNotFoundException)
            {
                await using (FileStream fs = File.Create("./cache/news/news.json"))
                {
                    fs.Write(new UTF8Encoding(true).GetBytes(JArray.FromObject(latestNews).ToString()));
                }
                return new List<News>();
            }
            
            await File.WriteAllTextAsync("./cache/news/news.json", JArray.FromObject(latestNews).ToString());
            
            List<News> oldNews = JsonSerializer.Deserialize<List<News>>(await File.ReadAllTextAsync("./cache/news/news.json") , ApiRequestBody.SerializeOptions);
            //List<News> oldNews = oldNewsJArray.Select(item => new News(JObject.FromObject(item))).ToList();
            return (from newItem in latestNews 
                    where oldNews.All(oldItem => Tools.GetIdFromUrl(newItem.Link) != Tools.GetIdFromUrl(oldItem.Link))
                    select newItem).ToList();
        }

        private static async Task<List<News>> GetLatestNews()
        {
            ApiRequestBody request = new();
            return await request.SendRequest<List<News>>("getRssNews");
            //JArray newNews =  await Tools.RequestApiJArray("getRssNews", new List<string>(), new List<string>());
            //return newNews.Select(news => new News(JObject.FromObject(news))).ToList();
        }
        
        private static Embed GetNewsEmbed(News news)
        {
            EmbedBuilder builder = new();

            string title = news.Title ?? "n.A";
            string description = news.Description ?? "n.A";
            string link = news.Link ?? "";

            builder.WithTitle(title).WithColor(Color.Blue);
            builder.AddField("description:", description);
            builder.WithAuthor("full story on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", link);
            builder.WithCurrentTimestamp();
            return builder.Build();
        }

        public static async Task SendNewNews()
        {
            Stopwatch watch = new(); watch.Start();
            foreach (News news in await GetNewNews())
            {
                await Tools.SendMessagesWithWebhook(x => x.NewsWebhookId != null,
                    x => x.NewsWebhookId, x=> x.NewsWebhookToken , GetNewsEmbed(news));
            }
            Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched news ({watch.ElapsedMilliseconds}ms)");
        }
    }
}
