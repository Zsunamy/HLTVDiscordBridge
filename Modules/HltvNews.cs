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

            List<News> oldNews = oldNewsJArray.Select(item => new News(JObject.FromObject(item))).ToList();
            return (from newItem in latestNews 
                    where oldNews.All(oldItem => Tools.GetIdFromUrl(newItem.link) != Tools.GetIdFromUrl(oldItem.link))
                    select newItem).ToList();
        }

        private static async Task<List<News>> GetLatestNews()
        {
            JArray newNews =  await Tools.RequestApiJArray("getRssNews", new List<string>(), new List<string>());
            return newNews.Select(news => new News(JObject.FromObject(news))).ToList();
        }
        
        private static Embed GetNewsEmbed(News news)
        {
            EmbedBuilder builder = new();

            string title = news.title ?? "n.A";
            string description = news.description ?? "n.A";
            string link = news.link ?? "";

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
