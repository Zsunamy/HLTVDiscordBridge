using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using Discord.Rest;
using Discord.Webhook;

namespace HLTVDiscordBridge
{
    public class ConfigClass
    {
        public string BotToken { get; set; }
        public int CheckResultsTimeInterval { get; set; }
        public string TopGGApiKey { get; set; }
        public string BotsGGApiKey { get; set; }
        public string APILink { get; set; }
        public string DatabaseLink { get; set; }
        public string Database { get; set; }
    }

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

    public class Config : ModuleBase<SocketCommandContext>
    {
        public static async void InitAllWebhooks(DiscordSocketClient client)
        {
            List<ServerConfig> configs = (await GetCollection().FindAsync(_ => true)).ToList();
            foreach (ServerConfig config in configs)
            {
                Stream icon = new FileStream("icon.png", FileMode.Open);
                IWebhook webhook = await ((SocketTextChannel)client.GetChannel(config.NewsChannelID)).CreateWebhookAsync("HLTV", icon);
                UpdateDefinition<ServerConfig> update =
                    Builders<ServerConfig>.Update
                        .Set(x => x.ResultWebhookId, webhook.Id)
                        .Set(x => x.ResultWebhookToken, webhook.Token)
                        .Set(x => x.EventWebhookId, webhook.Id)
                        .Set(x => x.EventWebhookToken, webhook.Token)
                        .Set(x => x.NewsWebhookId, webhook.Id)
                        .Set(x => x.NewsWebhookToken, webhook.Token);
                await GetCollection().UpdateOneAsync(x => x.NewsChannelID == config.NewsChannelID, update);
            }
        }
        public static IMongoCollection<ServerConfig> GetCollection()
        {
            MongoClient dbClient = new(LoadConfig().DatabaseLink);
            IMongoDatabase db = dbClient.GetDatabase(LoadConfig().Database);
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
            List<SocketTextChannel> channels = new();
            IMongoCollection<ServerConfig> collection = GetCollection();
            List<ServerConfig> configs = (await collection.FindAsync(_ => true)).ToList();

            foreach (ServerConfig cfg in configs)
            {
                if ((SocketTextChannel)client.GetChannel(cfg.NewsChannelID) != null)
                {
                    channels.Add((SocketTextChannel)client.GetChannel(cfg.NewsChannelID));
                }
            }
            return channels;
        }
        
        public static async Task<List<ServerConfig>> GetServerConfigs(Expression<Func<ServerConfig, bool>> filter)
        {
            return (await GetCollection().FindAsync(filter)).ToList();
        }

        /// <summary>
        /// Loads the general bot config
        /// </summary>
        /// <returns>Config</returns>
        public static ConfigClass LoadConfig()
        {
            XmlSerializer _xml;
            ConfigClass conf = new();
            _xml = new XmlSerializer(typeof(ConfigClass));
            FileStream stream = new("./config.xml", FileMode.Open);
            conf = (ConfigClass)_xml.Deserialize(stream);
            stream.Close();
            return conf;
        }

#region Commands
        [Command("set")]
        public async Task ChangeServerConfig_legacy(string option = "", [Remainder]string arg = "")
        {
            EmbedBuilder builder = new();

            IMongoCollection<ServerConfig> collection = GetCollection();

            UpdateDefinition<ServerConfig> update = null;

            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel)))
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }

            ServerConfig _cfg = GetServerConfig(Context.Guild);

            if (!(Context.User as SocketGuildUser).GuildPermissions.ManageGuild)
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the output-channel!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if(option == "")
            {
                builder.WithTitle("SYNTAX")
                    .WithColor(Color.Green)
                    .WithDescription($"You can change the following options by using /set [option] [new state]:")
                    .AddField("options:", "`stars`\n`featuredevents`\n`prefix`\n`newsoutput`\n`resultoutput`\n`eventoutput`", true)
                    .AddField("possible states:", "number between 0-5\ntrue/false\nany string\ntrue/false\ntrue/false\ntrue/false", true)
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            } else if(arg == "")
            {
                builder.WithTitle("syntax error")
                    .WithColor(Color.Red)
                    .WithDescription($"You can't change {option} to nothing! Please use a valid state: /set [option] [new state]!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            } 
            else
            {
                switch (option.ToLower())
                {
                    case "stars":
                    case "minstars":
                        if (ushort.TryParse(arg, out ushort newStars))
                        {
                            if (newStars >= 0 && newStars <= 5)
                            {
                                update = Builders<ServerConfig>.Update.Set(x => x.MinimumStars, newStars);
                                builder.WithColor(Color.Green)
                                    .WithTitle("SUCCESS")
                                    .WithDescription($"You successfully changed the minimum stars to output a HLTV match to `{newStars}`")
                                    .WithCurrentTimestamp()
                                    .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                            }
                            else
                            {
                                builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a number between 0-5")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                            }
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a number between 0-5")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        break;
                    case "featuredevents":
                        if (bool.TryParse(arg, out bool featuredevents))
                        {
                            update = Builders<ServerConfig>.Update.Set(x => x.OnlyFeaturedEvents, featuredevents);
                            string featured;
                            if (featuredevents) { featured = "only featured events"; }
                            else { featured = "all events"; }
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic event output to `{featured}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        break;
                    case "news":
                    case "newsoutput":
                        if (bool.TryParse(arg, out bool newsoutput))
                        {
                            update = Builders<ServerConfig>.Update.Set(x => x.NewsOutput, newsoutput);
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic news output to `{newsoutput}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        break;
                    case "result":
                    case "results":
                    case "resultoutput":
                        if (bool.TryParse(arg, out bool resultoutput))
                        {
                            update = Builders<ServerConfig>.Update.Set(x => x.ResultOutput, resultoutput);
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic result output to `{resultoutput}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        break;
                    case "event":
                    case "events":
                    case "eventoutput":
                        if (bool.TryParse(arg, out bool eventoutput))
                        {
                            update = Builders<ServerConfig>.Update.Set(x => x.EventOutput, eventoutput);
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic event output to `{eventoutput}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        }
                        break;
                    default:
                        builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{option.ToLower()} is not valid! Please state one of the following options:\n`stars`\n`featuredevents`\n`prefix`\n" +
                                $"`news`\n`results`\n`events`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(/*Context.Guild, Context.Client*/));
                        break;
                }
            }
            await collection.UpdateOneAsync(x => x.GuildID == Context.Guild.Id, update);
            await ReplyAsync(embed: builder.Build());
        }

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

            IMongoCollection<ServerConfig> collection = GetCollection();
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

            ServerConfig config = GetServerConfig((arg.User as SocketGuildUser)?.Guild);
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
                    update = Builders<ServerConfig>.Update.Set(x => x.ResultOutput, bool.Parse(value));
                    builder.WithColor(Color.Green)
                        .WithTitle("SUCCESS")
                        .WithDescription($"You successfully changed the automatic result output to `{value}`")
                        .WithCurrentTimestamp()
                        .WithFooter(Tools.GetRandomFooter());
                    break;
                case "events":
                    update = Builders<ServerConfig>.Update.Set(x => x.EventOutput, bool.Parse(value));
                    builder.WithColor(Color.Green)
                        .WithTitle("SUCCESS")
                        .WithDescription($"You successfully changed the automatic event output to `{value}`")
                        .WithCurrentTimestamp()
                        .WithFooter(Tools.GetRandomFooter());
                    break;
                case "news":
                    update = Builders<ServerConfig>.Update.Set(x => x.NewsOutput, bool.Parse(value));
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
            
            await collection.UpdateOneAsync(x => x.GuildID == (arg.User as SocketGuildUser).Guild.Id, update);
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
                collection.UpdateOne(x => x.GuildID == (arg.Channel as SocketTextChannel).Guild.Id, Builders<ServerConfig>.Update.Set(x => x.NewsChannelID, channel.Id));
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
        /// <param name="client">Bot Client</param>
        /// <param name="channel">Sets a custom Channel. null = default channel on guild</param>
        /// <param name="startup">Is this the startup?</param>
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

            ServerConfig _config = new();
            if(channel != null) { _config.NewsChannelID = channel.Id; }            
            _config.GuildID = guild.Id;
            _config.MinimumStars = 0;
            _config.OnlyFeaturedEvents = false;
            _config.EventOutput = true;
            _config.NewsOutput = true;
            _config.ResultOutput = true;

            collection.InsertOne(_config);

            if(channel == null) {
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
                    return;
                }
            }
            else 
            {
                try { 
                    await channel.SendMessageAsync(embed: builder.Build());
                    StatsUpdater.StatsTracker.MessagesSent += 1;
                    StatsUpdater.UpdateStats();
                }
                catch (Exception) { return; }
            }
            
        }

        public static async Task ServerconfigStartUp(DiscordSocketClient client)
        {
            IMongoCollection<ServerConfig> collection = GetCollection();
            foreach(SocketGuild guild in client.Guilds)
            {
                if (collection.Find(x => x.GuildID == guild.Id).CountDocuments() == 0)
                {
                    //await guild.LeaveAsync();
                    //Console.WriteLine($"Would have left: {guild.Name}");
                    Console.WriteLine($"found guild {guild.Name} with no config. Creating default.");
                    await Program.GetInstance().GuildJoined(guild);
                }
            }
        }
        public static async Task<GuildEmote> GetEmote(DiscordSocketClient client)
        {
            foreach (SocketGuild guild in client.Guilds)
            {
                if (guild.Id == 748637221300732076)
                {
                    return await guild.GetEmoteAsync(809082404324114522);
                }
            }
            return null;
        }
    }
}
