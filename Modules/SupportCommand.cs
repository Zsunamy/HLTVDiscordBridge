using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class SupportCommand : ModuleBase<SocketCommandContext>
    {
        public static async Task DispSupport(SocketSlashCommand arg)
        {
            EmbedBuilder builder = new();
            builder.WithColor(Color.Green)
                .WithTitle("Support")
                .WithDescription($"Do you have any inquiries or issues?\n\nContact us on [Github](https://github.com/Zsunamy/HLTVDiscordBridge/issues)\n" +
                $"or join our [discord support server](https://discord.gg/r2U23xu4z5).\n\n" +
                $"**Developers**\n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>\n\n" +
                $"Also feel free to [donate](https://www.patreon.com/zsunamy) us to support this project.\n " +
                $"Another quick and easy way to show us your support is by [voting](https://top.gg/bot/807182830752628766/vote) for this bot on " +
                $"[top.gg](https://top.gg/bot/807182830752628766) to increase awareness.")
                .WithCurrentTimestamp();
            /*StatsUpdater.StatsTracker.MessagesSent += 1;
            StatsUpdater.UpdateStats();*/
            await arg.RespondAsync(embed: builder.Build());
        }
    }
}
