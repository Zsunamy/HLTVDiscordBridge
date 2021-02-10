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
            doc.Load("./cache/newsneu.xml");
            XmlNodeList nodes = doc.GetElementsByTagName("item");
            XmlNodeList latestNews = nodes[0].ChildNodes;
            Console.WriteLine(latestNews.ToString());
            return null;
        }








        //Veraltet mit api
        public async Task<JObject> GetMessage()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/news");
            HttpClient http = new HttpClient();

            http.BaseAddress = URI;

            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            JArray jArr;
            try { jArr = JArray.Parse(await httpResponse.Content.ReadAsStringAsync()); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return null; }

            Directory.CreateDirectory("./cache");
            if (!File.Exists("./cache/news.txt"))
            {
                var stream = File.Create("./cache/news.txt");
                stream.Close();

                foreach (JToken jToken in jArr)
                {
                    File.AppendAllText("./cache/news.txt", JObject.Parse(jToken.ToString()).GetValue("link").ToString() + "\n");
                }
                return null;
            }
            string news = File.ReadAllText("./cache/news.txt");

            foreach (JToken jToken in jArr)
            {
                if (!news.Contains(JObject.Parse(jToken.ToString()).GetValue("link").ToString()))
                {
                    File.AppendAllText("./cache/news.txt", JObject.Parse(jToken.ToString()).GetValue("link").ToString() + "\n");
                    return JObject.Parse(jToken.ToString());
                }
                else
                {
                    continue;
                }
            }
            return null;
        }

        public Embed GetNews(JObject jObj)
        {
            var data = jObj;

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(data.GetValue("title").ToString())
                .WithColor(Color.Blue);       

            builder.AddField("description:", data.GetValue("description"));
            builder.WithAuthor("full story on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", data.GetValue("link").ToString());
            builder.WithCurrentTimestamp();

            return builder.Build();
        }

        public async Task aktHLTVNews(List<SocketTextChannel> channels)
        {
            //await GetNews(); 
            var msg = await GetMessage();
            if (msg != null)
            {
                Embed embed = GetNews(msg);
                foreach(SocketTextChannel channel in channels)
                {
                    try { await channel.SendMessageAsync("", false, embed); }
                    catch (Discord.Net.HttpException) { Console.WriteLine($"not enough permission in channel {channel}"); continue; }
                    
                }                
            }
        }
    }
}
