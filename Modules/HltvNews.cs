using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
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
                oldNewsJArray = JArray.Parse(File.ReadAllText("./cache/news/news.json"));
            }
            catch (JsonReaderException ex)
            {
                File.WriteAllText("./cache/news/news.json", JArray.FromObject(latestNews).ToString());
                return new List<News>();
            }
            catch (FileNotFoundException ex)
            {
                FileStream fs = File.Create("./cache/news/news.json");
                fs.Close();
                File.WriteAllText("./cache/news/news.json", JArray.FromObject(latestNews).ToString());
                return new List<News>();
            }
            
            File.WriteAllText("./cache/news/news.json", JArray.FromObject(latestNews).ToString());
            
            List<News> newsToSend = new();
            List<News> oldNews = new();
            foreach (JToken item in oldNewsJArray)
            {
                oldNews.Add(new News(JObject.FromObject(item)));
            }
            foreach (News newItem in latestNews)
            {
                var found = false;
                foreach (News oldItem in oldNews)
                {
                    if (Tools.GetIdFromUrl(newItem.link) == Tools.GetIdFromUrl(oldItem.link))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    newsToSend.Add(newItem);
                }
            }
            return newsToSend;
        }

        private static async Task<List<News>> GetLatestNews()
        {
            JArray newNews =  await Tools.RequestApiJArray("getRssNews", new List<string>(), new List<string>());
            List<News> newsList = new();
            foreach (JToken news in newNews)
            {
                newsList.Add(new News(JObject.FromObject(news)));
            }
            
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
                        try
                        {
                            await channel.SendMessageAsync(embed: embed);
                            StatsUpdater.StatsTracker.MessagesSent += 1;
                            StatsUpdater.UpdateStats();
                        }
                        catch (Discord.Net.HttpException)
                        {
                            Program.WriteLog($"not enough permission in channel {channel}");
                        }
                        catch (Exception e) {Program.WriteLog(e.ToString());}
                    }
                }                
            }
        }
    }
}
