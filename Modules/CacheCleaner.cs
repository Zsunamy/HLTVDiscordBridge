using System;
using System.IO;

namespace HLTVDiscordBridge.Modules;

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
            if (Directory.GetCreationTime(dir).AddDays(7).Date == DateTime.Now.Date)
            {
                foreach (string file in Directory.GetFiles(dir)) { File.Delete(file); }
                Directory.Delete(dir);
            }
        }
    }
}