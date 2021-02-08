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
        private Rendering.NameGenerator nameGenerator;
        private Rendering.KillfeedGenerator killfeedGenerator;

        public Killfeed()
        {
            nameGenerator = new Rendering.NameGenerator();
            killfeedGenerator = new Rendering.KillfeedGenerator();
        }

        [Command("killfeed")]
        public async Task KillfeedCommand(string firstPlayerName = "none", string secondPlayerName = "none", bool firstPlayerIsTerrorist = false, bool secondPlayerIsTerrorist = true, string weapon = "ak47", bool isHeadshot = true)
        {
            

            if(firstPlayerName == "none")
            {
                firstPlayerName = nameGenerator.GenerateName();
            } 
            if(secondPlayerName == "none")
            {
                secondPlayerName = nameGenerator.GenerateName();
            }

            killfeedGenerator.GenerateImage(firstPlayerName, secondPlayerName, firstPlayerIsTerrorist, secondPlayerIsTerrorist, weapon, isHeadshot);
            await Context.Channel.SendFileAsync("killfeed.png");
        }
    }
}
