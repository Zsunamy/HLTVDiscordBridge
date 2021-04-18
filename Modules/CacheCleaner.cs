using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;

namespace HLTVDiscordBridge.Modules
{
    public class CacheCleaner : ModuleBase<SocketCommandContext>
    {             
        public static void Cleaner(DiscordSocketClient client)
        {
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
                if (dir == "./cache/teamcards\\zsunamy") { continue; }
                if (Directory.GetCreationTime(dir).AddDays(7).Date == DateTime.Now.Date)
                {
                    foreach (string file in Directory.GetFiles(dir)) { File.Delete(file); }
                    Directory.Delete(dir);
                }
            }

            //ranking
            Directory.CreateDirectory("./cache/ranking");
            bool rankingDeleted = false;
            if (DateTime.Now.DayOfWeek == DayOfWeek.Tuesday && !rankingDeleted)
            {
                rankingDeleted = true;
                foreach(string file in Directory.GetFiles("./cache/ranking"))
                {
                    File.Delete(file);
                }
            } else if(DateTime.Now.DayOfWeek == DayOfWeek.Wednesday) { rankingDeleted = false; }

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
