using Discord.WebSocket;
using System;
using System.IO;

namespace HLTVDiscordBridge.Modules
{
    public static class CacheCleaner
    {             
        public static void Clean()
        {
            //delete player-cards after 7 days
            Directory.CreateDirectory("./cache/playercards");
            foreach (string dir in Directory.GetDirectories("./cache/playercards"))
            {
                if (Directory.GetCreationTime(dir).AddDays(7).Date == DateTime.Now.Date) 
                { 
                    foreach (string file in Directory.GetFiles(dir)) { File.Delete(file); }
                    Directory.Delete(dir); 
                }                
            }

            //delete team-cards after 7 days
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
            switch (DateTime.Now.DayOfWeek)
            {
                case DayOfWeek.Tuesday when !rankingDeleted:
                {
                    rankingDeleted = true;
                    foreach(string file in Directory.GetFiles("./cache/ranking"))
                    {
                        File.Delete(file);
                    }

                    break;
                }
                case DayOfWeek.Wednesday:
                    rankingDeleted = false;
                    break;
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
