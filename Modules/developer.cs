using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules;

public static class Developer
{
    public static async Task Update(SocketSlashCommand arg, DiscordSocketClient client)
    {
        await arg.DeferAsync();
        EmbedBuilder builder = new();

        List<SocketTextChannel> channels = await Config.GetChannelsLegacy(client);

        foreach (SocketTextChannel channel in channels)
        {
            builder.WithTitle($"Update: {arg.Data.Options.First().Value}")
                .WithDescription(arg.Data.Options.Last().Value + $"\nDo you have any inquiries or issues? [Contact us!](https://github.com/Zsunamy/HLTVDiscordBridge/issues)\n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>\n" +
                                 $"Also feel free to [donate](https://www.patreon.com/zsunamy) us to support this project.\n Another quick and easy way to show us your support is by [voting](https://top.gg/bot/807182830752628766/vote) for this bot on [top.gg](https://top.gg/bot/807182830752628766) to increase awareness.")
                .WithColor(Color.Green)
                .WithCurrentTimestamp();
            try
            {
                await channel.SendMessageAsync(embed: builder.Build());
                StatsUpdater.StatsTracker.MessagesSent += 1;
                StatsUpdater.UpdateStats();
            }
            catch (Discord.Net.HttpException)
            {
                Program.WriteLog($"not enough permission in channel {channel}");
            }
        }
        await arg.ModifyOriginalResponseAsync(msg => msg.Content = "update sent");
    }
}