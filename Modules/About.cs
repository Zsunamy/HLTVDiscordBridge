using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class About : ModuleBase<SocketCommandContext>
    {
        [Command("about")]
        public async Task DispAbout()
        {
            ComponentBuilder componentBuilder = new();
            componentBuilder.WithButton("playerstats","id");

            EmbedBuilder builder = new();
            builder.WithColor(Color.Green)
                .WithTitle("About Us")
                .WithDescription($"Do you have any inquiries or issues? [Contact us!](https://github.com/Zsunamy/HLTVDiscordBridge/issues)\n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>\n" +
                $"Also feel free to [donate](https://www.patreon.com/zsunamy) us to support this project.\n Another quick and easy way to show us your support is by [voting](https://top.gg/bot/807182830752628766/vote) for this bot on [top.gg](https://top.gg/bot/807182830752628766) to increase awareness.")
                .WithCurrentTimestamp();
            StatsUpdater.StatsTracker.MessagesSent += 1;
            StatsUpdater.UpdateStats();
            await ReplyAsync(embed: builder.Build(), components: componentBuilder.Build());
        }

        public static Embed buttonClicked(SocketMessageComponent arg)
        {
            EmbedBuilder builder = new();
            builder.WithTitle("tit");
            return builder.Build();
        }
    }
}
