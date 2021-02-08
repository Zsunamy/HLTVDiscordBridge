﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class About : ModuleBase<SocketCommandContext>
    {
        [Command("about")]
        public async Task DispAbout()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Green)
                .WithTitle("About Us")
                .WithDescription($"Any Questions or issues? Contact us!\n https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>")
                .WithCurrentTimestamp();
            await ReplyAsync("", false, builder.Build());
        }
    }
}