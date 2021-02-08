using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HLTVDiscordBridge.Modules
{
    public class MassKillfeed : ModuleBase<SocketCommandContext>
    {
        private Rendering.NameGenerator nameGenerator;
        private Rendering.KillfeedGenerator killfeedGenerator;
        private Random random;

        public MassKillfeed()
        {
            nameGenerator = new Rendering.NameGenerator();
            killfeedGenerator = new Rendering.KillfeedGenerator();
            random = new Random();
        }

        [Command("masskillfeed")]
        public async Task MassKillfeedCommand()
        {
            for(uint i = 0; i < 10; i++)
            {
                bool firstPlayerTerrorist = random.Next(2) == 1;
                killfeedGenerator.GenerateImage(nameGenerator.GenerateName(), nameGenerator.GenerateName(), firstPlayerTerrorist, !firstPlayerTerrorist, nameGenerator.GetRandomWeapon(), random.Next(2) == 1);
                await Context.Channel.SendFileAsync("killfeed.png");
            }
        }
    }
}
