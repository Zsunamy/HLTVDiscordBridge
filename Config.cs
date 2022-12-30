using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using System;
using System.Collections.Generic;
using System.IO;
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
    public ulong GuildID { get; set; }
    public ulong NewsChannelID { get; set; }
    public ulong? NewsWebhookId { get; set; }
    public ulong? ResultWebhookId { get; set; }
    public ulong? EventWebhookId { get; set; }
    public string NewsWebhookToken { get; set; }
    public string ResultWebhookToken { get; set; }
    public string EventWebhookToken { get; set; }
    public ushort MinimumStars { get; set; }
    public bool OnlyFeaturedEvents { get; set; }
    public bool NewsOutput { get; set; }
    public bool ResultOutput { get; set; }
    public bool EventOutput { get; set; }

    public IEnumerable<Webhook> GetWebhooks()
    {
        return new[]
        {
            new Webhook { Id = NewsWebhookId, Token = NewsWebhookToken },
            new Webhook { Id = ResultWebhookId, Token = ResultWebhookToken },
            new Webhook { Id = EventWebhookId, Token = EventWebhookToken }
        }.Where(webhook => webhook.Id != null);
    }
    
    public async Task<Webhook> CheckIfConfigUsesWebhookOfChannel(ITextChannel channel)
    {
        Webhook[] webhooks =
        {
            new Webhook{Id = ResultWebhookId, Token = ResultWebhookToken},
            new Webhook{Id = NewsWebhookId, Token = NewsWebhookToken},
            new Webhook{Id = EventWebhookId, Token = EventWebhookToken}
        };
        return (from webhook in await channel.GetWebhooksAsync()
            select new Webhook { Id = webhook.Id, Token = webhook.Token }).FirstOrDefault(channelWebhook => 
            webhooks.Aggregate(false, (b, currentWebhook) => 
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
            Stream icon = new FileStream("icon.png", FileMode.Open);
            Webhook updateWebhook;
            ITextChannel channel = (ITextChannel)client.GetChannel(config.NewsChannelID);
            try
            {
                IWebhook webhook = await channel.CreateWebhookAsync("HLTV", icon);
                updateWebhook = new Webhook{Id = webhook.Id, Token = webhook.Token};
            }
            catch (Exception e)
            {
                updateWebhook = new Webhook{Id = null, Token = ""};
                Console.WriteLine(e.ToString());
                await channel.SendMessageAsync(
                    $"ERROR: Failed to create webhook with the following message {e.Message}\n" + 
                    "The Bot is probably missing permissions to manage webhooks.\n" +
                    "Please give the bot these permissions and then setup all channels manually using the /set command");
            }
            List<UpdateDefinition<ServerConfig>> updates = new();
                
            if (config.ResultOutput)
            {
                updates.Add(Builders<ServerConfig>.Update.Set(x => x.ResultWebhookId, updateWebhook.Id)
                    .Set(x => x.ResultWebhookToken, updateWebhook.Token));
            }
            if (config.EventOutput)
            {
                updates.Add(Builders<ServerConfig>.Update.Set(x => x.EventWebhookId, updateWebhook.Id)
                    .Set(x => x.EventWebhookToken, updateWebhook.Token));
            }

            if (config.NewsOutput)
            {
                updates.Add(Builders<ServerConfig>.Update.Set(x => x.NewsWebhookId, updateWebhook.Id)
                    .Set(x => x.NewsWebhookToken, updateWebhook.Token));
            }

            UpdateDefinition<ServerConfig> update = Builders<ServerConfig>.Update.Combine(updates);
            await GetCollection().UpdateOneAsync(x => x.NewsChannelID == config.NewsChannelID, update);
        }
        Console.WriteLine("Finished initializing all webhooks!");
    }

    private static async Task<UpdateDefinition<ServerConfig>> SetWebhook(bool enable, Expression<Func<ServerConfig, ulong?>> filterId,
        Expression<Func<ServerConfig, string>> filterToken , ulong? guildId, ITextChannel channel)
    {
        FilterDefinition<ServerConfig> configFilter = Builders<ServerConfig>.Filter.Eq(x => x.GuildID, guildId);
        ServerConfig config = GetCollection().FindSync(configFilter).ToList().First();
        Webhook webhookInDatabase = new Webhook{ Id = filterId.Compile()(config), Token = filterToken.Compile()(config)};
        Webhook newWebhook;
        if (enable)
        {
            Webhook multiWebhook = await config.CheckIfConfigUsesWebhookOfChannel(channel);
            if (webhookInDatabase.CheckIfWebhookIsUsed(config))
            {
                await webhookInDatabase.Delete();
            }
            if (multiWebhook == null)
            {
                IWebhook bufferWebhook = await channel.CreateWebhookAsync("HLTV", new FileStream("icon.png", FileMode.Open));
                newWebhook = new Webhook{Id = bufferWebhook.Id, Token = bufferWebhook.Token};
            }
            else {
                newWebhook = multiWebhook;
            }
        }
        else
        {
            if (!webhookInDatabase.CheckIfWebhookIsUsed(config))
            {
                await webhookInDatabase.Delete();
            }
            newWebhook = new Webhook{Id = null, Token = ""};
        }
            
        return Builders<ServerConfig>.Update.Set(filterId, newWebhook.Id)
            .Set(filterToken, newWebhook.Token);
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
        Embed embed;
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

        SocketSlashCommandDataOption option = arg.Data.Options.First();
        object value =  option.Options.Count != 0 ? option.Options.First().Value : null;
        ITextChannel channel = null;
        if (option.Options.Count != 0)
        {
            try
            {
                channel = (ITextChannel)option.Options.First().Value;
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
                update = await SetWebhook(true, x => x.NewsWebhookId, x => x.NewsWebhookToken,
                    arg.GuildId, channel);
                embed = GetSetEmbed($"You successfully changed the news notifications output to <#{channel!.Id}>.");
                break;
            case "results":
                update = await SetWebhook(true, x => x.ResultWebhookId, x => x.ResultWebhookToken,
                    arg.GuildId, channel);
                embed = GetSetEmbed($"You successfully changed the news notifications output to <#{channel!.Id}>.");
                break;
            case "events":
                update = await SetWebhook(true, x => x.EventWebhookId, x => x.EventWebhookToken,
                    arg.GuildId, channel);
                embed = GetSetEmbed($"You successfully changed the news notifications output to <#{channel!.Id}>.");
                break;
            case "featuredeventsonly":
                update = Builders<ServerConfig>.Update.Set(x => x.OnlyFeaturedEvents, (bool)value!);
                string featured = (bool)value ? "only featured events" : "all events";
                embed = GetSetEmbed($"From now on you will only receive event notifications for {featured}.");
                break;
            case "disable":
                update = value switch
                {
                    "news" => await SetWebhook(false, x => x.NewsWebhookId, x => x.NewsWebhookToken,
                        arg.GuildId, null),
                    "results" => await SetWebhook(false, x => x.ResultWebhookId, x => x.ResultWebhookToken,
                        arg.GuildId, null),
                    "events" => await SetWebhook(false, x => x.EventWebhookId, x => x.EventWebhookToken,
                        arg.GuildId, null),
                    _ => throw new ArgumentOutOfRangeException(nameof(arg), "Invalid Parameter. This is a Bug!")
                };

                embed = GetSetEmbed($"You successfully disabled all notifications about `{value}`.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(arg), "Invalid Parameter. This is a Bug!");
        }
        await GetCollection().UpdateOneAsync(x => x.GuildID == arg.GuildId, update);
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }

    /// <summary>
    /// Creates channel and sets it as output for HLTVNews and HLTVMatches
    /// </summary>
    /// <param name="guild">Guild on which the Channel should be created</param>
    /// <param name="channel">Sets a custom Channel. null = default channel on guild</param>
    public static async Task GuildJoined(SocketGuild guild, SocketTextChannel channel = null)
    {
        IMongoCollection<ServerConfig> collection = GetCollection();

        EmbedBuilder builder = new();
        if (channel == null)
        {                
            channel = guild.DefaultChannel;
            string channelMention;
            string guildName;
            if (channel == null)
            {
                guildName = "n.A"; channelMention = "n.A";
            }
            else
            {
                guildName = guild.Name; channelMention = channel.Mention;
            }
            builder.WithTitle("Init")
                .WithDescription($"Thanks for adding the HLTVDiscordBridge to {guildName}. {channelMention} is set as default output for HLTV-NEWS. " +
                                 $"Type !help for more info about how to proceed. If there are any questions or issues feel free to contact us!\n" +
                                 $"https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>")
                .WithCurrentTimestamp()
                .WithColor(Color.DarkBlue);
        }
        else
        {
            builder.WithTitle("Init")
                .WithDescription($"Success! You are now using the channel {channel.Mention} as default output for HLTV-NEWS")
                .WithCurrentTimestamp()
                .WithColor(Color.DarkBlue);
        }

        ServerConfig config = new ServerConfig{GuildID = guild.Id, MinimumStars = 0};
                 
        config.GuildID = guild.Id;
        config.MinimumStars = 0;
        config.OnlyFeaturedEvents = false;
        config.EventOutput = true;
        config.NewsOutput = true;
        config.ResultOutput = true;

        await collection.InsertOneAsync(config);

        if (channel == null) {
            builder.WithDescription($"Thanks for adding the HLTVDiscordBridge to {guild.Name}. By default all notifications are enabled. " +
                                    "You can use the /set command to customize which notifications you want to receive. " +
                                    "To disable them you need to use /set disable subcommand. " +
                                    "Type /help for more information about all features this bot provides. " +
                                    "If there are any questions or issues feel free to contact us!\n" +
                                    "[GitHub](https://github.com/Zsunamy/HLTVDiscordBridge/issues) [discord](https://discord.gg/r2U23xu4z5)");
            try
            {
                await guild.Owner.SendMessageAsync(embed: builder.Build());
            }
            catch (Exception)
            {
                //TODO send message to server owner.
                throw new NotImplementedException();
            }
        }
        else 
        {
            await channel.SendMessageAsync(embed: builder.Build());
            StatsUpdater.StatsTracker.MessagesSent += 1;
            StatsUpdater.UpdateStats();
        }
            
    }

    public static async Task ServerConfigStartUp(DiscordSocketClient client)
    {
        IMongoCollection<ServerConfig> collection = GetCollection();
        foreach (SocketGuild guild in client.Guilds)
        {
            if (await collection.Find(x => x.GuildID == guild.Id).CountDocumentsAsync() == 0)
            {
                Console.WriteLine($"found guild {guild.Name} with no config. Creating default.");
                await Program.GetInstance().GuildJoined(guild);
            }
        }
    }
}