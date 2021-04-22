using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
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
                    foreach(SocketGuildUser user in guild.Users)
                    {
                        if(user.IsBot) { continue; }
                        totalUser++;
                    }
                }
                await ReplyAsync($"{Context.Client.Guilds.Count} server and {totalUser} user");
            }
        }

        [Command("update")]
        public async Task Update(string version, [Remainder] string message)
        {
            if (Context.User.Id == 255000770707980289 || Context.User.Id == 224037892387766272 || Context.User.Id == 248110264610848778) 
            {
                EmbedBuilder builder = new();
                
                foreach (SocketTextChannel channel in await Config.GetChannels(Context.Client))
                {
                    ServerConfig config = Config.GetServerConfig(channel);
                    string newMessage = message.Replace("<prefix>", config.Prefix);
                    builder.WithTitle($"Update: {version}")
                        .WithDescription(newMessage + $"\nDo you have any inquiries or issues? [Contact us!](https://github.com/Zsunamy/HLTVDiscordBridge/issues)\n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>\n" +
                $"Also feel free to [donate](https://www.patreon.com/zsunamy) us to support this project.\n Another quick and easy way to show us your support is by [voting](https://top.gg/bot/807182830752628766/vote) for this bot on [top.gg](https://top.gg/bot/807182830752628766) to increase awareness.")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp();
                    try { await channel.SendMessageAsync(embed: builder.Build()); }
                    catch (Discord.Net.HttpException)
                    {
                        Program.WriteLog($"not enough permission in channel {channel}");
                    }
                }
            }            
        }
    }    
}
