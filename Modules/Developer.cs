using System;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
        List<ServerConfig> configs = Config.GetCollection().FindSync(_ => true).ToList();
        List<Task> status = configs.Select(config => Task.Run(async () =>
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
                        Console.WriteLine(ex);
                        if (ex is Discord.Net.HttpException)
                        {
                            await Program.Log(new LogMessage(LogSeverity.Warning, nameof(Developer), $"not enough permission in channel {channel!.Name}", ex));
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    await webhook.ToDiscordWebhookClient().SendMessageAsync(embeds: new[] { embed });
                }
            })).ToList();
        StatsTracker.GetStats().MessagesSent += status.ToList().Count;
        await Task.WhenAll(status);

        await arg.ModifyOriginalResponseAsync(msg => msg.Content = "update sent!");
    }
}