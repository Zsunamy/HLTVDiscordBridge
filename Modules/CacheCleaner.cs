using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace HLTVDiscordBridge.Modules
{
    public class CacheCleaner : ModuleBase<SocketCommandContext>
    {
        public void Cleaner(DiscordSocketClient client)
        {
            //ServerConfigs
            Directory.CreateDirectory("./cache/serverconfig");
            foreach(string file in Directory.GetFiles("./cache/serverconfig"))
            {
                bool todelete = true;
                foreach(SocketGuild guild in client.Guilds)
                {
                    if (file.Contains(guild.Id.ToString()))
                    {
                        todelete = false;
                        break;
                    }
                }
                if (todelete)
                {
                    File.Delete(file);
                }
            }
            //delete playercards after 7 days
            Directory.CreateDirectory("./cache/playercards");
            foreach (string dir in Directory.GetDirectories("./cache/playercards"))
            {
                if (Directory.GetCreationTime(dir).AddDays(7).Date == DateTime.Now.Date)
                {
                    foreach (string file in Directory.GetFiles(dir)) { File.Delete(file); }
                    Directory.Delete(dir);
                }
            }
            //delete teamcards after 7 days
            Directory.CreateDirectory("./cache/teamcards");
            foreach (string dir in Directory.GetDirectories("./cache/teamcards"))
            {
                if (Directory.GetCreationTime(dir).AddDays(7).Date == DateTime.Now.Date)
                {
                    foreach (string file in Directory.GetFiles(dir)) { File.Delete(file); }
                    Directory.Delete(dir);
                }
            }
            //ranking
            Directory.CreateDirectory("./cache/ranking");
            if (DateTime.Now.DayOfWeek == DayOfWeek.Tuesday)
            {
                foreach(string file in Directory.GetFiles("./cache/ranking"))
                {
                    File.Delete(file);
                }
            }
            //live matches
            Directory.CreateDirectory("./cache/livematches");
            foreach(string matchFile in Directory.GetFiles("./cache/livematches"))
            {
                if(File.GetCreationTimeUtc(matchFile).AddHours(12).CompareTo(DateTime.Now.ToUniversalTime()) < 0)
                {
                    File.Delete(matchFile);
                }
            }
        }
    }
}
