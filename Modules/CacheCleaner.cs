using Discord.Commands;
using System;
using System.IO;

namespace HLTVDiscordBridge.Modules
{
    public class CacheCleaner : ModuleBase<SocketCommandContext>
    {             
        public void Cleaner()
        {
            //matches
            string matches = File.ReadAllText("./cache/matchIDs.txt");
            if((matches.Split("\n").Length - 1) > 200)
            {
                File.WriteAllText("./cache/matchIDs.txt", matches.Substring(matches.Split("\n")[0].Length + 1));
            }
            //news
            string news = File.ReadAllText("./cache/news.txt");
            if((news.Split("\n").Length - 1) > 20)
            {
                File.WriteAllText("./cache/news.txt", news.Substring(news.Split("\n")[0].Length + 1));
            }
            string stars = File.ReadAllText("./cache/upcoming.json");
            if (stars.Split(",").Length - 1 > 126)
            {
                File.WriteAllText("./cache/upcoming.json", "[\n" + stars.Substring(stars.Split("}")[0].Length + 4));
            }
        }
    }
}
