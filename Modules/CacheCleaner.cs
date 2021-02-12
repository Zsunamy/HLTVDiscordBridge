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
            //matches
            string matches = File.ReadAllText("./cache/matchIDs.txt");
            if((matches.Split("\n").Length - 1) > 400)
            {
                File.WriteAllText("./cache/matchIDs.txt", matches.Substring(matches.Split("\n")[0].Length + 1));
            }
            //upcoming.json
            string upcoming = File.ReadAllText("./cache/upcoming.json");
            JArray jArr = JArray.Parse(upcoming);
            if (jArr.Count > 100)
            {
                jArr.Remove(jArr[0]);
                File.WriteAllText("./cache/upcoming.json", jArr.ToString());
            }
            //ServerConfigs
            bool todelete = true;
            Directory.CreateDirectory("./cache/serverconfig");
            foreach(string file in Directory.GetFiles("./cache/serverconfig"))
            {
                todelete = true;
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
                if (Directory.GetCreationTime(dir).AddDays(7).Date == DateTime.Now.Date) { Directory.Delete(dir); }                
            }
            
        }
    }
}
