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
    }


    public class HltvNews : ModuleBase<SocketCommandContext>
    {
        //official RSS Feed       
        public async Task<News> GetNews()
        {
            HttpClient http = new HttpClient();
            HttpRequestMessage req = new HttpRequestMessage();
            req.RequestUri = new Uri("https://www.hltv.org/rss/news");
            HttpResponseMessage res = await http.SendAsync(req);
            string result = await res.Content.ReadAsStringAsync();
            if (!File.Exists("./cache/news.xml")) { var fs = File.Create("./cache/news.xml");  fs.Close(); }
            if (File.ReadAllText("./cache/news.xml") == result) { return null; }
            File.WriteAllText("./cache/news.xml", await res.Content.ReadAsStringAsync());

            XmlDocument doc = new XmlDocument();
            doc.Load("./cache/news.xml");
            XmlNodeList nodes = doc.GetElementsByTagName("item");
            XmlNodeList latestNews = nodes[0].ChildNodes;

            News news = new News();
            news.title = latestNews[0].InnerText;
            news.description = latestNews[1].InnerText;
            news.link = latestNews[2].InnerText;
            return news;
        }

        public Embed GetNews(News news)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(news.title)
                .WithColor(Color.Blue);       

            builder.AddField("description:", news.description);
            builder.WithAuthor("full story on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", news.link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }

        public async Task aktHLTVNews(List<SocketTextChannel> channels)
        {
            News bam = await GetNews();
            if (bam != null)
            {
                Embed embed = GetNews(bam);
                foreach(SocketTextChannel channel in channels)
                {
#if RELEASE
                    try { await channel.SendMessageAsync("", false, embed); }
                    catch (Discord.Net.HttpException) { Console.WriteLine($"not enough permission in channel {channel}"); continue; }   
#endif
                }                
            }
        }
    }
}
