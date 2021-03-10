using Discord;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace HLTVDiscordBridge.Modules
{
    public class Tools
    {
        public static EmbedFooterBuilder GetRandomFooter (SocketGuild guild, DiscordSocketClient client)
        {
            EmbedFooterBuilder builder = new();
            string[] footerStrings = File.ReadAllText("./cache/footer.txt").Split("\n");
            Random _rnd = new();
            Config _cfg = new();
            string footerString = footerStrings[_rnd.Next(0, footerStrings.Length)];
            if (footerString.Contains("<prefix>")) { footerString = footerString.Replace("<prefix>", _cfg.GetServerConfig(guild).Prefix); }
            if (footerString.Contains("<servercount>")) { footerString = footerString.Replace("<servercount>", client.Guilds.Count.ToString()); }
            int totalUser = 0;
            foreach (SocketGuild g in client.Guilds)
            {
                totalUser += g.Users.Count;
            }
            if (footerString.Contains("<playercount>")) { footerString = footerString.Replace("<playercount>", totalUser.ToString()); }
            builder.Text = footerString;
            return builder;
        }
    }
}
