using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HLTVDiscordBridge.Modules
{
    public class News
    {
        public string title { get; set; }
        public string description { get; set; }
        public string link { get; set; }
        public ushort id { get; set; }
    }


    public class HltvNews : ModuleBase<SocketCommandContext>
    {
        //official RSS Feed       
        public static async Task<News> GetNews()
        {
            HttpClient http = new();
            HttpRequestMessage req = new();
            req.RequestUri = new Uri("https://www.hltv.org/rss/news");
            HttpResponseMessage res = await http.SendAsync(req);
            string result = await res.Content.ReadAsStringAsync();
            if(result == "error")
            {

            }
            if (!File.Exists("./cache/news/news.xml")) { var fs = File.Create("./cache/news/news.xml");  fs.Close(); }

            if (File.ReadAllText("./cache/news/news.xml") == result) { return null; }
            File.WriteAllText("./cache/news/news.xml", result);

            XmlDocument doc = new();
            doc.Load("./cache/news/news.xml");
            XmlNodeList nodes = doc.GetElementsByTagName("item");
            XmlNodeList latestNews = nodes[0].ChildNodes;
            News news = new();
            news.title = latestNews[0].InnerText;
            news.description = latestNews[1].InnerText;
            news.link = latestNews[2].InnerText;
            news.id = ushort.Parse(news.link.Substring(26, 5));
            
            return news;
        }

        public static Embed GetNewsEmbed(News news)
        {
            EmbedBuilder builder = new();
            builder.WithTitle(news.title)
                .WithColor(Color.Blue);       

            builder.AddField("description:", news.description);
            builder.WithAuthor("full story on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", news.link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }

        public static async Task AktHLTVNews(List<SocketTextChannel> channels)
        {
            News bam = await GetNews(); 
            if(bam == null) { return; }

            string[] ids = File.ReadAllLines("./cache/news/ids.txt");
            bool sent = false;

            foreach (string id in ids) { if(id == bam.id.ToString()) { sent = true; } }

            if (!sent)
            {
                StatsUpdater.StatsTracker.NewsSent += 1;
                StatsUpdater.UpdateStats();
                Embed embed = GetNewsEmbed(bam);
                File.WriteAllText("./cache/news/ids.txt", bam.id.ToString() + "\n" + File.ReadAllText("./cache/news/ids.txt"));
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
