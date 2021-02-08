using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvNews : ModuleBase<SocketCommandContext>
    {

        public async Task<JObject> GetMessage()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/news");
            HttpClient http = new HttpClient();

            http.BaseAddress = URI;

            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            JArray jArr = JArray.Parse(await httpResponse.Content.ReadAsStringAsync());

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
            var msg = await GetMessage();
            if (msg != null)
            {
                Embed embed = GetNews(msg);
                foreach(SocketTextChannel channel in channels)
                {
                    try { await channel.SendMessageAsync("", false, embed); }
                    catch (Discord.Net.HttpException) { Console.WriteLine($"not enough permission in channel {channel}"); }
                    
                }                
            }
        }
    }
}
