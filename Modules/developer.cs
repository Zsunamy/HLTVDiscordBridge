using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Developer : ModuleBase<SocketCommandContext>
    {
        Config _cfg = new Config();

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

        [Command("update")]
        public async Task Update([Remainder] string message)
        {
            if (Context.User.Id == 255000770707980289 || Context.User.Id == 224037892387766272 || Context.User.Id == 248110264610848778) 
            {
                _cfg = new Config();
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("UPDATE")
                    .WithDescription(message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                foreach (SocketTextChannel channel in _cfg.GetChannels(Context.Client))
                {
                    await channel.SendMessageAsync("", false, builder.Build());
                }
            }
            
        }
    }    
}
