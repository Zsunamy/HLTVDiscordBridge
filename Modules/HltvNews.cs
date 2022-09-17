using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Modules
{


    public class HltvNews : ModuleBase<SocketCommandContext>
    {
        
        //official RSS Feed       
        public static async Task<List<News>> GetNewNews()
        {
            if (!File.Exists("./cache/news/news.json")) { var fs = File.Create("./cache/news/news.json");  fs.Close(); }
            var oldNewsJArray = JArray.Parse(File.ReadAllText("./cache/news/news.json"));
            List<News> latestNews = await GetLatestNews();
            List<News> newsToSend = new();
            List<News> oldNews = new();
            foreach (var item in oldNewsJArray)
            {
                oldNews.Add(new News(JObject.FromObject(item)));
            }
            foreach (var newItem in latestNews)
            {
                var found = false;
                foreach (var oldItem in oldNews)
                {
                    if (newItem.link == oldItem.link)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) {newsToSend.Add(newItem);}
            }
            return newsToSend;

            /*HttpClient http = new();
            HttpRequestMessage req = new();
            req.RequestUri = new Uri("https://www.hltv.org/rss/news");
            req.Headers.Add("User-Agent", "curl/7.54.0");
            HttpResponseMessage res = await http.SendAsync(req);
            string result = await res.Content.ReadAsStringAsync();

            if (!File.Exists("./cache/news/news.xml")) { var fs = File.Create("./cache/news/news.xml");  fs.Close(); }
            if (File.ReadAllText("./cache/news/news.xml") == result) { return null; }

            File.WriteAllText("./cache/news/news.xml", result);

            XmlDocument doc = new();
            doc.Load("./cache/news/news.xml");
            XmlNodeList nodes = doc.GetElementsByTagName("item");
            XmlNodeList latestNews = nodes[0].ChildNodes;
            News news = new(latestNews);           
            
            return news;*/
        }

        private static async Task<List<News>> GetLatestNews()
        {
            var newNews =  await Tools.RequestApiJArray("getNews", new List<string>(), new List<string>());
            List<News> newsList = new();
            foreach (var news in newNews)
            {
                newsList.Add(new News(JObject.FromObject(news)));
            }

            File.WriteAllText("./cache/news/news.json", JArray.FromObject(newNews).ToString());
            return newsList;
        }

        private static Embed GetNewsEmbed(News news)
        {
            EmbedBuilder builder = new();

            string title = news.title ?? "n.A";
            string description = news.description ?? "n.A";
            string link = news.link ?? "";

            builder.WithTitle(title)
                .WithColor(Color.Blue);       

            builder.AddField("description:", description);
            builder.WithAuthor("full story on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }

        public static async Task SendNewNews(List<SocketTextChannel> channels)
        {
            List<News> newsToSend = await GetNewNews();
            foreach (var news in newsToSend)
            {
                StatsUpdater.StatsTracker.NewsSent += 1;
                StatsUpdater.UpdateStats();
                Embed embed = GetNewsEmbed(news);
                foreach (SocketTextChannel channel in channels)
                {
                    ServerConfig config = Config.GetServerConfig(channel);
                    if(config.NewsOutput)
                    {
                        try { 
                            await channel.SendMessageAsync(embed: embed);
                            StatsUpdater.StatsTracker.MessagesSent += 1;
                            StatsUpdater.UpdateStats();
                        }
                        catch (Discord.Net.HttpException) { Program.WriteLog($"not enough permission in channel {channel}"); continue; }
                    }
                }                
            }
        }
    }
}
