using Discord;
using Discord.Commands;
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

namespace HLTVDiscordBridge
{
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
    }

    public static class Config
    {
        public static async void InitAllWebhooks(DiscordSocketClient client)
        {
            List<ServerConfig> configs = (await GetCollection().FindAsync(_ => true)).ToList();
            foreach (ServerConfig config in configs)
            {
                Stream icon = new FileStream("icon.png", FileMode.Open);
                Webhook updateWebhook;
                SocketTextChannel channel = (SocketTextChannel)client.GetChannel(config.NewsChannelID);
                try
                {
                    IWebhook webhook = await channel.CreateWebhookAsync("HLTV", icon);
                    updateWebhook = new Webhook(webhook.Id, webhook.Token);
                }
                catch (Exception e)
                {
                    updateWebhook = new Webhook(null, "");
                    Console.WriteLine(e.ToString());
                    await channel.SendMessageAsync(
                        $"ERROR: Failed to create webhook with the following message {e.Message}\n" + 
                        $"The Bot is probably missing permissions to manage webhooks.\n" +
                        $"Please give the bot these permissions and then setup all channels manually using the /set command");
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
            Expression<Func<ServerConfig, string>> filterToken , SocketTextChannel channel, ulong? guildId)
        {
            FilterDefinition<ServerConfig> configFilter = Builders<ServerConfig>.Filter.Eq(x => x.GuildID, guildId);
            ServerConfig config = GetCollection().FindSync(configFilter).ToList().First();
            Webhook webhookInDatabase = new(filterId.Compile()(config), filterToken.Compile()(config));
            Webhook newWebhook;
            if (enable)
            {
                Webhook? multiWebhook = await Tools.CheckChannelForWebhook(channel, config);
                if (!Tools.CheckIfWebhookIsUsed(webhookInDatabase, config))
                {
                    await Tools.DeleteWebhook(webhookInDatabase);
                }
                if (multiWebhook == null)
                {
                    IWebhook bufferWebhook = await channel.CreateWebhookAsync("HLTV", new FileStream("icon.png", FileMode.Open));
                    newWebhook = new Webhook(bufferWebhook.Id, bufferWebhook.Token);
                }
                else {
                    newWebhook = (Webhook)multiWebhook;
                }
            }
            else
            {
                if (!Tools.CheckIfWebhookIsUsed(webhookInDatabase, config))
                {
                    await Tools.DeleteWebhook(webhookInDatabase);
                }
                newWebhook = new Webhook(null, "");
            }
            
            return Builders<ServerConfig>.Update.Set(filterId, newWebhook.Id)
                    .Set(filterToken, newWebhook.Token);
        }
        public static IMongoCollection<ServerConfig> GetCollection()
        {
            MongoClient dbClient = new(BotConfig.GetBotConfig().DatabaseLink);
            IMongoDatabase db = dbClient.GetDatabase(BotConfig.GetBotConfig().Database);
            return db.GetCollection<ServerConfig>("serverconfig");
        }
        public static ServerConfig GetServerConfig(SocketTextChannel channel)
        {
            IMongoCollection<ServerConfig> collection = GetCollection();
            return collection.Find(x => x.NewsChannelID == channel.Id).First();
        }
        public static ServerConfig GetServerConfig(SocketGuild guild)
        {
            IMongoCollection<ServerConfig> collection = GetCollection();
            return collection.Find(x => x.GuildID == guild.Id).First();
        }
        public static async Task<List<SocketTextChannel>> GetChannelsLegacy(DiscordSocketClient client)
        {
            IMongoCollection<ServerConfig> collection = GetCollection();
            List<ServerConfig> configs = (await collection.FindAsync(_ => true)).ToList();

            return (from cfg in configs where (SocketTextChannel)client.GetChannel(cfg.NewsChannelID) != null
                select (SocketTextChannel)client.GetChannel(cfg.NewsChannelID)).ToList();
        }
        
        public static async Task<List<ServerConfig>> GetServerConfigs(Expression<Func<ServerConfig, bool>> filter)
        {
            return (await GetCollection().FindAsync(filter)).ToList();
        }

        #region Commands
        [Command("set")]

        public static async Task ChangeServerConfig(SocketSlashCommand arg)
        {
            await arg.DeferAsync();
            EmbedBuilder builder = new();
            if (!(arg.User is SocketGuildUser))
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("This command is exclusive to guilds and cannot be used in DMs!")
                    .WithCurrentTimestamp();
                await arg.ModifyOriginalResponseAsync(msg => msg.Embed = builder.Build());
                return;
            }            
            
            UpdateDefinition<ServerConfig> update = null;

            if (!(arg.User as SocketGuildUser).GuildPermissions.ManageGuild)
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the settings!")
                    .WithCurrentTimestamp();
                await arg.ModifyOriginalResponseAsync(msg => msg.Embed = builder.Build());
                return;
            }
            
            SocketSlashCommandDataOption option = arg.Data.Options.First();
            string value = option.Options.First().Value.ToString();
            
            switch (option.Name.ToLower())
            {
                case "stars":
                    update = Builders<ServerConfig>.Update.Set(x => x.MinimumStars, ushort.Parse(value));
                    builder.WithColor(Color.Green)
                        .WithTitle("SUCCESS")
                        .WithDescription($"You successfully changed the minimum stars to output a HLTV match to `{value}`")
                        .WithCurrentTimestamp()
                        .WithFooter(Tools.GetRandomFooter());
                    break;
                case "results":
                    update = await SetWebhook(bool.Parse(value), x => x.ResultWebhookId, x => x.ResultWebhookToken,
                        (SocketTextChannel)arg.Channel, arg.GuildId);
                    builder.WithColor(Color.Green)
                        .WithTitle("SUCCESS")
                        .WithDescription($"You successfully changed the automatic result output to `{value}`")
                        .WithCurrentTimestamp()
                        .WithFooter(Tools.GetRandomFooter()); 
                    break;
                case "events":
                    update = await SetWebhook(bool.Parse(value), x => x.EventWebhookId, x => x.EventWebhookToken,
                        (SocketTextChannel)arg.Channel, arg.GuildId);
                    builder.WithColor(Color.Green)
                        .WithTitle("SUCCESS")
                        .WithDescription($"You successfully changed the automatic event output to `{value}`")
                        .WithCurrentTimestamp()
                        .WithFooter(Tools.GetRandomFooter());
                    break;
                case "news":
                    update = await SetWebhook(bool.Parse(value), x => x.NewsWebhookId, x => x.NewsWebhookToken,
                        (SocketTextChannel)arg.Channel, arg.GuildId);
                    builder.WithColor(Color.Green)
                        .WithTitle("SUCCESS")
                        .WithDescription($"You successfully changed the automatic news output to `{value}`")
                        .WithCurrentTimestamp()
                        .WithFooter(Tools.GetRandomFooter());
                    break;
                case "featuredeventsonly":
                    update = Builders<ServerConfig>.Update.Set(x => x.OnlyFeaturedEvents, bool.Parse(value));
                    string featured = bool.Parse(value) ? "only featured events" : "all events";
                    builder.WithColor(Color.Green)
                        .WithTitle("SUCCESS")
                        .WithDescription($"You successfully changed the automatic event output to `{featured}`")
                        .WithCurrentTimestamp()
                        .WithFooter(Tools.GetRandomFooter());
                    break;
            }
            await GetCollection().UpdateOneAsync(x => x.GuildID == arg.GuildId, update);
            await arg.ModifyOriginalResponseAsync(msg => msg.Embed = builder.Build());
        }

        public static async Task InitTextChannel(SocketSlashCommand arg)
        {
            IGuildChannel channel = arg.Data.Options.First().Value as IGuildChannel;
            EmbedBuilder builder = new();
            if (channel.GetType() != typeof(SocketTextChannel))
            {
                builder.WithTitle("ERROR")
                    .WithDescription("Please select a valid channel!")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
            }   
            else
            {
                IMongoCollection<ServerConfig> collection = GetCollection();
                await collection.UpdateOneAsync(x => x.GuildID == (arg.Channel as SocketTextChannel).Guild.Id, Builders<ServerConfig>.Update.Set(x => x.NewsChannelID, channel.Id));
                builder.WithTitle("Init")
                        .WithDescription($"Success! You are now using the channel {(channel as SocketTextChannel).Mention} as default output for HLTV-NEWS")
                        .WithCurrentTimestamp()
                        .WithColor(Color.DarkBlue);
            }            
            await arg.RespondAsync(embed: builder.Build());
        }
        #endregion


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
                if (channel == null) { guildName = "n.A"; channelMention = "n.A"; }
                else { guildName = guild.Name; channelMention = channel.Mention; }
                builder.WithTitle("Init")
                    .WithDescription($"Thanks for adding the HLTVDiscordBridge to {guildName}. {channelMention} is set as default output for HLTV-NEWS. " +
                    $"Type !help for more info about how to proceed. If there are any questions or issues feel free to contact us!\n" +
                    $"https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>")
                    .WithCurrentTimestamp()
                    .WithColor(Color.DarkBlue);
            } else
            {
                builder.WithTitle("Init")
                    .WithDescription($"Success! You are now using the channel {channel.Mention} as default output for HLTV-NEWS")
                    .WithCurrentTimestamp()
                    .WithColor(Color.DarkBlue);
            }

            ServerConfig config = new();
            if(channel != null) { config.NewsChannelID = channel.Id; }            
            config.GuildID = guild.Id;
            config.MinimumStars = 0;
            config.OnlyFeaturedEvents = false;
            config.EventOutput = true;
            config.NewsOutput = true;
            config.ResultOutput = true;

            await collection.InsertOneAsync(config);

            if (channel == null) {
                builder.WithDescription($"Thanks for adding the HLTVDiscordBridge to {guild.Name}. To set a default HLTV-News output channel, type /init " +
                    $"in a channel of your choice, but make sure that the bot has enough permission to access and send messages in that channel. " +
                    $"Type !help for more info about how to proceed. If there are any questions or issues feel free to contact us!\n" +
                    $"https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>");
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
}
