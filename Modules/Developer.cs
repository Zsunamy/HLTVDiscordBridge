using System;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.Repository;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Modules;

public static class Developer
{
    public static async Task NotifyCriticalError(LogMessage log)
    {
        BotConfig config = BotConfig.GetBotConfig();
        await Program.GetInstance().Client.GetGuild(config.DeveloperServer).DefaultChannel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("A critical Error occured")
                .WithDescription($"{log.Message}\nWith the following Exception-message: {log.Exception.Message}" +
                                 $"\n{log.Exception}")
                .Build());
    } 
    public static async Task Update(SocketSlashCommand arg)
    {
        Embed embed = new EmbedBuilder()
            .WithTitle($"Update: {arg.Data.Options.First().Value}")
            .WithDescription(arg.Data.Options.Last().Value +
                             "\nDo you have any inquiries or issues? Contact us! [GitHub](https://github.com/Zsunamy/HLTVDiscordBridge/issues) " +
                             "[discord](https://discord.gg/r2U23xu4z5)\n" +
                             "Also feel free to [donate](https://www.patreon.com/zsunamy) us to support this project.\n" +
                             "Another quick and easy way to show us your support is by " +
                             "[voting](https://top.gg/bot/807182830752628766/vote) for this bot on [top.gg](https://top.gg/bot/807182830752628766) to increase awareness.")
            .WithColor(Color.Green)
            .WithCurrentTimestamp().Build();

        List<ServerConfig> configs = await ServerConfigRepository.GetAll();

        foreach (ServerConfig config in configs)
        {
            Webhook webhook = config.GetWebhooks().FirstOrDefault();
            if (webhook == null)
            {
                SocketTextChannel channel = null;
                try
                {
                    channel = Program.GetInstance().Client.GetGuild(config.GuildId).DefaultChannel;
                    await channel.SendMessageAsync(embed: embed);
                }
                catch (Exception ex)
                {
                    StatsTracker.GetStats().MessagesSent -= 1;
                    if (ex is Discord.Net.HttpException)
                    {
                        await Program.Log(new LogMessage(LogSeverity.Warning, nameof(Developer),
                            $"not enough permission in channel {channel!.Name}", ex));
                    }
                    else
                    {
                        await Program.Log(new LogMessage(LogSeverity.Error, nameof(Developer), ex.Message, ex));
                        throw;
                    }
                }
            }
            else
                await webhook.ToDiscordWebhookClient().SendMessageAsync(embeds: new[] { embed });
        }

        StatsTracker.GetStats().MessagesSent += configs.Count;
        await arg.ModifyOriginalResponseAsync(msg => msg.Content = "update sent!");
    }
}