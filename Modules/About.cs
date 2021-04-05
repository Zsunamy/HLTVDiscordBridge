using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class About : ModuleBase<SocketCommandContext>
    {
        [Command("about")]
        public async Task DispAbout()
        {
            EmbedBuilder builder = new();
            builder.WithColor(Color.Green)
                .WithTitle("About Us")
                .WithDescription($"Any Questions or issues? [Contact us!](https://github.com/Zsunamy/HLTVDiscordBridge/issues)\n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>\n" +
                $"Also feel free to [donate](https://www.patreon.com/zsunamy) us a cup of coffee")
                .WithCurrentTimestamp();
            await ReplyAsync(embed: builder.Build());
        }
    }
}
