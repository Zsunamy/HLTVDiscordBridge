using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
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
                string servernames = "";
                foreach (SocketGuild guild in Context.Client.Guilds)
                {
                    totalUser += guild.Users.Count;
                    servernames += $"{guild.Name} | {guild.Users.Count}\n";
                }
                await ReplyAsync($"{Context.Client.Guilds.Count} server and {totalUser} user \n\n{servernames}");
            }
        }

        [Command("update")]
        public async Task Update(string version, [Remainder] string message)
        {
            if (Context.User.Id == 255000770707980289 || Context.User.Id == 224037892387766272 || Context.User.Id == 248110264610848778) 
            {
                _cfg = new Config();
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle($"UPDATE {version}")
                    .WithDescription(message + "\nIf you are experiencing any issues feel free to write us an [issue](https://github.com/Zsunamy/HLTVDiscordBridge/issues)\n" +
                    "Also feel free to [donate](https://www.patreon.com/zsunamy) us a cup of coffee")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                foreach (SocketTextChannel channel in await _cfg.GetChannels(Context.Client))
                {
                    try { await channel.SendMessageAsync(embed: builder.Build()); }
                    catch (Discord.Net.HttpException)
                    {
                        Console.WriteLine($"not enough permission in channel {channel}");
                    }
                }
            }            
        }
    }    
}
