using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Developer : ModuleBase<SocketCommandContext>
    {
        [Command("servercount")]
        public async Task ServerCount()
        {
            if(Context.User.Id == 248110264610848778 || Context.User.Id == 224037892387766272 || Context.User.Id == 255000770707980289)
            {
                int totalUser = 0;
                foreach (SocketGuild guild in Context.Client.Guilds)
                {
                    totalUser += guild.Users.Count;
                }
                await ReplyAsync($"{Context.Client.Guilds.Count} server and {totalUser} user");
            }
        }
    }    
}
