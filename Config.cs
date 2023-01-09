using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Linq;
using System.Linq.Expressions;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge;

public class ServerConfig
{
    [BsonId]
    public ObjectId Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong NewsChannelID { get; set; }
    public Webhook News { get; set; }
    public Webhook Results { get; set; }
    public Webhook Events { get; set; }
    public int MinimumStars { get; set; }
    public bool OnlyFeaturedEvents { get; set; }
    public bool NewsOutput { get; set; }
    public bool ResultOutput { get; set; }
    public bool EventOutput { get; set; }

    public IEnumerable<Webhook> GetWebhooks()
    {
        return new[] { News, Results, Events }.Where(webhook => webhook != null);
    }

    public async Task<Webhook> CheckIfConfigUsesWebhookOfChannel(ITextChannel channel)
    {
        return (from webhook in await channel.GetWebhooksAsync()
            select new Webhook { Id = webhook.Id, Token = webhook.Token }).FirstOrDefault(channelWebhook => 
            GetWebhooks().Aggregate(false, (b, currentWebhook) => 
                (currentWebhook.Id == channelWebhook.Id && currentWebhook.Token == channelWebhook.Token) || b));
    }
}

public static class Config
{
    public static async Task InitAllWebhooks(DiscordSocketClient client)
    {
        List<ServerConfig> configs = (await GetCollection().FindAsync(_ => true)).ToList();
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await channel.SendMessageAsync(
                    $"ERROR: Failed to create webhook with the following message `{ex.Message}`\n" + 
                    "The Bot is probably missing permissions to manage webhooks.\n" +
                    "Please give the bot these permissions and then setup all channels manually using the /set command");
            }
        }
        Console.WriteLine("Finished initializing all webhooks!");
    }

    private static async Task<UpdateDefinition<ServerConfig>> SetWebhook(bool enable, Expression<Func<ServerConfig, Webhook>> getWebhook,
        ulong? guildId, ITextChannel channel)
    {
        FilterDefinition<ServerConfig> configFilter = Builders<ServerConfig>.Filter.Eq(x => x.GuildId, guildId);
        ServerConfig config = GetCollection().FindSync(configFilter).First();
        Webhook webhookInDatabase = getWebhook.Compile()(config);
        Webhook newWebhook;
        if (enable)
        {
            Webhook multiWebhook = await config.CheckIfConfigUsesWebhookOfChannel(channel);
            if (webhookInDatabase != null && !webhookInDatabase.CheckIfWebhookIsUsed(config))
                await webhookInDatabase.Delete();
            
            if (multiWebhook == null)
                newWebhook = await Webhook.CreateWebhook(channel);
            else
                newWebhook = multiWebhook;
        }
        else
        {
            if (webhookInDatabase != null && !webhookInDatabase.CheckIfWebhookIsUsed(config))
                await webhookInDatabase.Delete();
            
            newWebhook = null;
        }
            
        return Builders<ServerConfig>.Update.Set(getWebhook, newWebhook);
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
            
        UpdateDefinition<ServerConfig> update;
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
                update = Builders<ServerConfig>.Update.Set(x => x.MinimumStars, (int)value!);
                embed = GetSetEmbed($"You successfully changed the minimum stars to receive a result notification to `{value}`.");
                break;
            case "news":
                update = await SetWebhook(true, x => x.News, arg.GuildId, channel);
                embed = GetSetEmbed($"You successfully changed the news notifications output to <#{channel!.Id}>.");
                break;
            case "results":
                update = await SetWebhook(true, x => x.Results, arg.GuildId, channel);
                embed = GetSetEmbed($"You successfully changed the result notifications output to <#{channel!.Id}>.");
                break;
            case "events":
                update = await SetWebhook(true, x => x.Events, arg.GuildId, channel);
                embed = GetSetEmbed($"You successfully changed the event notifications output to <#{channel!.Id}>.");
                break;
            case "featuredeventsonly":
                update = Builders<ServerConfig>.Update.Set(x => x.OnlyFeaturedEvents, (bool)value!);
                string featured = (bool)value ? "only featured events" : "all events";
                embed = GetSetEmbed($"From now on you will only receive event notifications for {featured}.");
                break;
            case "disable":
                update = value switch
                {
                    "news" => await SetWebhook(false, x => x.News, arg.GuildId, null),
                    "results" => await SetWebhook(false, x => x.Results, arg.GuildId, null),
                    "events" => await SetWebhook(false, x => x.Events, arg.GuildId, null),
                    _ => throw new ArgumentOutOfRangeException(nameof(arg), "Invalid Parameter. This is a Bug!")
                };

                embed = GetSetEmbed($"You successfully disabled all notifications about `{value}`.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(arg), "Invalid Parameter. This is a Bug!");
        }
        await GetCollection().UpdateOneAsync(x => x.GuildId == arg.GuildId, update);
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
        ServerConfig config = new ServerConfig { GuildId = guild.Id, MinimumStars = 0, OnlyFeaturedEvents = false };
        config.EventOutput = true;
        config.NewsOutput = true;
        config.ResultOutput = true;
        try
        {
            webhook = await Webhook.CreateWebhook(guild.DefaultChannel);
        }
        finally
        {
            config.News = webhook;
            config.Results = webhook;
            config.Events = webhook;
            await GetCollection().InsertOneAsync(config);
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
            if (!await GetCollection().FindSync(x => x.GuildId == guild.Id).AnyAsync())
            {
                Console.WriteLine($"found guild {guild.Name} with no config. Creating default.");
                await Program.GetInstance().GuildJoined(guild);
            }
        }

        foreach (ServerConfig config in GetCollection().Find( _ => true).ToList().Where(config => client.GetGuild(config.GuildId) == null))
        {
            //TODO Testing
            Console.WriteLine("Found serverconfig but bot is not on server; Deleting");
            // await GetCollection().DeleteOneAsync(x => x.GuildID == config.GuildID);
        }
    }
}