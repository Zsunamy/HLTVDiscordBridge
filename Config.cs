using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;
using HLTVDiscordBridge.Notifications;
using HLTVDiscordBridge.Repository;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge;

public static class Config
{
    public static async Task InitAllWebhooks(DiscordSocketClient client)
    {
        List<ServerConfig> configs = await ServerConfigRepository.GetAll();
        foreach (ServerConfig config in configs)
        {
            ITextChannel channel = (ITextChannel)client.GetChannel(config.NewsChannelID);
            try
            {
                Webhook updateWebhook = await Webhook.CreateWebhook(channel);
                List<UpdateDefinition<ServerConfig>> updates = new();
            
                if (config.NewsOutput)
                    updates.Add(Builders<ServerConfig>.Update.Set(x => x.News, updateWebhook));
                
                if (config.ResultOutput)
                    updates.Add(Builders<ServerConfig>.Update.Set(x => x.Results, updateWebhook));
                
                if (config.EventOutput)
                    updates.Add(Builders<ServerConfig>.Update.Set(x => x.Events, updateWebhook));
                

                UpdateDefinition<ServerConfig> update = Builders<ServerConfig>.Update.Combine(updates);
                await GetCollection().UpdateOneAsync(x => x.NewsChannelID == config.NewsChannelID, update);
                await Program.Log(new LogMessage(LogSeverity.Info, "created all Webhooks", "InitAllWebhooks"));
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Warning, nameof(Config), ex.Message, ex));
                await channel.SendMessageAsync(
                    $"ERROR: Failed to create webhook with the following message `{ex.Message}`\n" + 
                    "The Bot probably doesn't have the required permissions to manage webhooks.\n" +
                    "Please give the bot these permissions and then setup all channels manually using the /set command");
            }
        }

        await Program.Log(new LogMessage(LogSeverity.Info, "Initialization of Webhooks",
            "Finished initializing all webhooks!"));
    }
    
    public static IMongoCollection<ServerConfig> GetCollection()
    {
        IMongoDatabase db = Program.DbClient.GetDatabase(BotConfig.GetBotConfig().Database);
        return db.GetCollection<ServerConfig>("serverconfig");
    }

    private static Embed GetSetEmbed(string description)
    {
        return new EmbedBuilder().WithColor(Color.Green)
            .WithTitle("SUCCESS")
            .WithDescription(description)
            .WithCurrentTimestamp()
            .WithFooter(Tools.GetRandomFooter()).Build();
    }

    public static async Task ChangeServerConfig(SocketSlashCommand arg)
    {
        if (arg.GuildId == null)
        {
            await arg.ModifyOriginalResponseAsync(msg => msg.Embed = new EmbedBuilder()
                .WithTitle("error")
                .WithColor(Color.Red)
                .WithDescription("This command is exclusive to guilds and cannot be used in DMs!")
                .WithCurrentTimestamp().Build());
            return;
        }            
        
        ServerConfig config = await ServerConfigRepository.GetConfigOrNull((ulong)arg.GuildId);
        Embed embed;

        SocketSlashCommandDataOption option = arg.Data.Options.First();
        object value =  option.Options.Count != 0 ? option.Options.First().Value : null;
        ITextChannel channel = null;
        if (value != null)
        {
            try
            {
                channel = (ITextChannel)value;
            }
            catch (InvalidCastException) {}
        }
        else
        {
            channel = (ITextChannel)arg.Channel;
        }

        switch (option.Name.ToLower())
        {
            case "stars":
                config.MinimumStars = Convert.ToInt32(value!);
                await ServerConfigRepository.Update(config);
                embed = GetSetEmbed($"You successfully changed the minimum stars to receive a result notification to `{value}`.");
                break;
            case "news":
                await NewsNotifier.Instance.Enroll(await ServerConfigRepository.GetConfigOrNull((ulong)arg.GuildId), channel);
                embed = GetSetEmbed($"You successfully changed the news notifications output to <#{channel!.Id}>.");
                break;
            case "results":
                await ResultNotifier.Instance.Enroll(await ServerConfigRepository.GetConfigOrNull((ulong)arg.GuildId), channel);
                embed = GetSetEmbed($"You successfully changed the result notifications output to <#{channel!.Id}>.");
                break;
            case "events":
                await EventNotifier.Instance.Enroll(await ServerConfigRepository.GetConfigOrNull((ulong)arg.GuildId), channel);
                embed = GetSetEmbed($"You successfully changed the event notifications output to <#{channel!.Id}>.");
                break;
            case "featuredeventsonly":
                config.OnlyFeaturedEvents = (bool)value!;
                await ServerConfigRepository.Update(config);
                string featured = (bool)value ? "only featured events" : "all events";
                embed = GetSetEmbed($"From now on you will only receive event notifications for {featured}.");
                break;
            case "disable":
                switch (option.Options.First().Value)
                {
                    case "news":
                        await NewsNotifier.Instance.Cancel(await ServerConfigRepository.GetConfigOrNull((ulong)arg.GuildId));
                        break;
                    case "results":
                        await ResultNotifier.Instance.Cancel(await ServerConfigRepository.GetConfigOrNull((ulong)arg.GuildId));
                        break;
                    case "events":
                        await EventNotifier.Instance.Cancel(await ServerConfigRepository.GetConfigOrNull((ulong)arg.GuildId));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(arg), "Invalid Parameter. This is a Bug!");
                }
                
                embed = GetSetEmbed($"You successfully disabled all notifications about `{value}`.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(arg), "Invalid Parameter. This is a Bug!");
        }
        
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }

    public static async Task SendMessageAfterServerJoin(SocketGuild guild, Embed embed)
    {
        try
        {
            await guild.DefaultChannel.SendMessageAsync(embed: embed);
            StatsTracker.GetStats().MessagesSent += 1;
        }
        catch (Discord.Net.HttpException)
        {
            try
            {
                await guild.Owner.SendMessageAsync(embed: embed);
                await guild.Owner.SendMessageAsync("It looks like the bot doesn't have permissions to send messages " +
                                                   "in the default text-channel. Since most all notifications are handled " +
                                                   "with webhooks, this shouldn't cause a problem. You can use the /set command " +
                                                   "to change or disable notifications.");
                StatsTracker.GetStats().MessagesSent += 1;
            }
            catch (Discord.Net.HttpException) {}
        }
    }
    
    public static async Task GuildJoined(SocketGuild guild)
    {
        Webhook webhook = null;
        ServerConfig config = new ServerConfig
        {
            GuildId = guild.Id,
            MinimumStars = 0,
            OnlyFeaturedEvents = false,
            News = null,
            Events = null,
            Results = null,
            EventOutput = true,
            NewsOutput = true,
            ResultOutput = true
        };
        try
        {
            webhook = await Webhook.CreateWebhook(guild.DefaultChannel);
        }
        finally
        {
            config.News = webhook;
            config.Results = webhook;
            config.Events = webhook;
            await ServerConfigRepository.Insert(config);
        }

        Embed embed = new EmbedBuilder().WithColor(Color.Green)
            .WithDescription(
                $"Thanks for adding the HLTVDiscordBridge to {guild.Name}. By default all notifications are enabled in the " +
                $"channel <#{guild.DefaultChannel}> " +
                "You can use the /set command to customize which notifications you want to receive and to select a different channel." +
                "To disable them you need to use /set disable subcommand. " +
                "Type /help for more information about all features this bot provides. " +
                "If there are any questions or issues feel free to contact us!\n" +
                "[GitHub](https://github.com/Zsunamy/HLTVDiscordBridge/issues) [discord](https://discord.gg/r2U23xu4z5)")
            .Build();
        await SendMessageAfterServerJoin(guild, embed);
    }

    public static async Task ServerConfigStartUp()
    {
        DiscordSocketClient client = Program.GetInstance().Client;
        foreach (SocketGuild guild in client.Guilds)
        {
            if (!await ServerConfigRepository.Exists(guild.Id))
            {
                await Program.Log(new LogMessage(LogSeverity.Info, nameof(Config),
                    $"found guild {guild.Name} with no config. Creating default."));
                await Program.GetInstance().GuildJoined(guild);
            }
        }

        foreach (ServerConfig config in (await ServerConfigRepository.GetAll()).Where(config => client.GetGuild(config.GuildId) == null))
        {
            //TODO Testing
            await Program.Log(new LogMessage(LogSeverity.Info, nameof(Config),
                "Found server-configuration but bot is not on server; Deleting"));
            //TODO needs testing and more safety
            // await GetCollection().DeleteOneAsync(x => x.GuildID == config.GuildID);
        }
    }
}