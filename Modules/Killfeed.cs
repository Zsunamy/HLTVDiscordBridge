using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Killfeed : ModuleBase<SocketCommandContext>
    {
        [Command("killfeed")]
        public async Task RenderKillfeed(string firstPlayerName = "BOT Bjöööan", bool isTerrorist = false)
        {
            KillfeedGenerator.KillfeedGenerator gen = new KillfeedGenerator.KillfeedGenerator();
            gen.GenerateImage(firstPlayerName, isTerrorist);
            await Context.Channel.SendFileAsync("killfeed.png");
        }
    }
}
