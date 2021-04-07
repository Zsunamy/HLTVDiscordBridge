using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HLTVDiscordBridge
{
    public class ConfigClass
    {
        public string BotToken { get; set; }
        public int CheckResultsTimeInterval { get; set; }
        public string TopGGApiKey { get; set; }
        public string BotsGGApiKey { get; set; }
        public string APILink { get; set; }
    }

    public class ServerConfig
    {
        public ulong GuildID { get; set; }
        public ulong NewsChannelID { get; set; }
        public ushort MinimumStars { get; set; }
        public bool OnlyFeaturedEvents { get; set; }
        public string Prefix { get; set; }
        public bool NewsOutput { get; set; }
        public bool ResultOutput { get; set; }
        public bool EventOutput { get; set; }
    }

    public class Config : ModuleBase<SocketCommandContext>
    {

        XmlSerializer _xml;
        /// <summary>
        /// Loads the generel bot config
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
        public async Task ChangeServerConfig(string option = "", [Remainder]string arg = "")
        {
            ServerConfig _cfg = GetServerConfig(Context.Guild);
            FileStream fs = new("./cache/serverconfig/" + Context.Guild.Id + ".xml", FileMode.Create);
            XmlSerializer _xml = new(typeof(ServerConfig));
            EmbedBuilder builder = new();
            if(Context.Channel.GetType().Equals(typeof(SocketDMChannel)))
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                _xml.Serialize(fs, _cfg);
                fs.Close();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if(!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the output-channel!")
                    .WithCurrentTimestamp();
                _xml.Serialize(fs, _cfg);
                fs.Close();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if(option == "")
            {
                builder.WithTitle("SYNTAX")
                    .WithColor(Color.Green)
                    .WithDescription($"You can change the following options by using {_cfg.Prefix}set [option] [new state]:")
                    .AddField("options:", "`stars`\n`featuredevents`\n`prefix`\n`newsoutput`\n`resultoutput`\n`eventoutput`", true)
                    .AddField("possible states:", "number between 0-5\ntrue/false\nany string\ntrue/false\ntrue/false\ntrue/false", true)
                    .WithCurrentTimestamp();
                _xml.Serialize(fs, _cfg);
                fs.Close();
                await ReplyAsync(embed: builder.Build());
                return;
            } else if(arg == "")
            {
                builder.WithTitle("syntax error")
                    .WithColor(Color.Red)
                    .WithDescription($"You can't change {option} to nothing! Please use a valid state: {_cfg.Prefix}set [option] [new state]!")
                    .WithCurrentTimestamp();
                _xml.Serialize(fs, _cfg);
                fs.Close();
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
                                _cfg.MinimumStars = newStars;
                                builder.WithColor(Color.Green)
                                    .WithTitle("SUCCESS")
                                    .WithDescription($"You successfully changed the minimum stars to output a HLTV match to `{newStars}`")
                                    .WithCurrentTimestamp()
                                    .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                            }
                            else
                            {
                                builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a number between 0-5")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                            }
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a number between 0-5")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        break;
                    case "featuredevents":
                        if (bool.TryParse(arg, out bool featuredevents))
                        {
                            _cfg.OnlyFeaturedEvents = featuredevents;
                            string featured;
                            if (featuredevents) { featured = "only featured events"; }
                            else { featured = "all events"; }
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic event output to `{featured}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        break;
                    case "prefix":
                        _cfg.Prefix = arg;
                        builder.WithColor(Color.Green)
                            .WithTitle("SUCCESS")
                            .WithDescription($"You successfully changed the prefix to `{arg}`")
                            .WithCurrentTimestamp()
                            .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        break;
                    case "news":
                    case "newsoutput":
                        if (bool.TryParse(arg, out bool newsoutput))
                        {
                            _cfg.NewsOutput = newsoutput;
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic news output to `{newsoutput}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        break;
                    case "result":
                    case "results":
                    case "resultoutput":
                        if (bool.TryParse(arg, out bool resultoutput))
                        {
                            _cfg.ResultOutput = resultoutput;
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic result output to `{resultoutput}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        break;
                    case "event":
                    case "events":
                    case "eventoutput":
                        if (bool.TryParse(arg, out bool eventoutput))
                        {
                            _cfg.EventOutput = eventoutput;
                            builder.WithColor(Color.Green)
                                .WithTitle("SUCCESS")
                                .WithDescription($"You successfully changed the automatic event output to `{eventoutput}`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        else
                        {
                            builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{arg} is not valid! Please state a boolean value (true/false)")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        }
                        break;
                    default:
                        builder.WithColor(Color.Red)
                                .WithTitle("error")
                                .WithDescription($"{option.ToLower()} is not valid! Please state one of the following options:\n`stars`\n`featuredevents`\n`prefix`\n" +
                                $"`news`\n`results`\n`events`")
                                .WithCurrentTimestamp()
                                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
                        break;
                }
            }
            
            _xml.Serialize(fs, _cfg);
            fs.Close();
            await ReplyAsync(embed: builder.Build());
        }


        [Command("init")]
        public async Task InitTextChannel(SocketTextChannel channel = null)
        {
            EmbedBuilder builder = new();
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel)))
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                builder.WithTitle("error")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the output-channel!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (channel == null)
            {
                channel = (SocketTextChannel)Context.Channel;
            }
            await GuildJoined(Context.Guild, channel);
        }
        #endregion


        /// <summary>
        /// Creates channel and sets it as output for HLTVNews and HLTVMatches
        /// </summary>
        /// <param name="guild">Guild on which the Channel should be created</param>
        /// <param name="client">Bot Client</param>
        /// <param name="channel">Sets a custom Channel. null = default channel on guild</param>
        /// <param name="startup">Is this the startup?</param>
        public async Task GuildJoined(SocketGuild guild, SocketTextChannel channel = null, bool startup = false)
        {
            EmbedBuilder builder = new();
            if (channel == null)
            {                
                channel = guild.DefaultChannel;
                string channelMention;
                string guildName;
                if (channel == null) { guildName = "n.A"; channelMention = "n.A"; }
                else { guildName = guild.Name; channelMention = channel.Mention; }
                builder.WithTitle("INIT")
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
            _config.Prefix = "!";
            _config.EventOutput = true;
            _config.NewsOutput = true;
            _config.ResultOutput = true;

            _xml = new XmlSerializer(typeof(ServerConfig));
            Directory.CreateDirectory("./cache/serverconfig");
            if(File.Exists($"./cache/serverconfig/{guild.Id}.xml") && startup) { return; }
            FileStream stream = new($"./cache/serverconfig/{guild.Id}.xml", FileMode.Create);  
            _xml.Serialize(stream, _config);            
            stream.Close();
            if(channel == null) {
                builder.WithDescription($"Thanks for adding the HLTVDiscordBridge to {guild.Name}. To set a default HLTV-News output channel, type !init " +
                    $"in a channel of your choice, but make sure that the bot has enough permission to access and send messages in that channel. " +
                    $"Type !help for more info about how to proceed. If there are any questions or issues feel free to contact us!\n" +
                    $"https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>");
                try { await guild.Owner.SendMessageAsync(embed: builder.Build()); }
                catch (Discord.Net.HttpException) { return; }
            }
            else { await channel.SendMessageAsync(embed: builder.Build()); }
            
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

        /// <summary>
        /// Gets all HLTV output channels of the client
        /// </summary>
        /// <param name="client">acting client</param>
        /// <returns>List<SocketTextChannel> of all channels</returns>
        public async Task<List<SocketTextChannel>> GetChannels (DiscordSocketClient client)
        {
            List<SocketTextChannel> channel = new();
            ServerConfig _config = new();
            _xml = new XmlSerializer(typeof(ServerConfig));
            foreach (SocketGuild guild in client.Guilds)
            {
                if (!File.Exists($"./cache/serverconfig/{guild.Id}.xml")) { await GuildJoined(guild); }                
                FileStream fs = new($"./cache/serverconfig/{guild.Id}.xml", FileMode.Open);
                _config = (ServerConfig)_xml.Deserialize(fs);
                fs.Close();
                if ((SocketTextChannel)client.GetChannel(_config.NewsChannelID) != null) { channel.Add((SocketTextChannel)client.GetChannel(_config.NewsChannelID)); }                
            }
            return channel;
        }
        /// <summary>
        /// Gets the Serverconfig
        /// </summary>
        /// <param name="channel">HLTV output channel</param>
        /// <returns>ServerConfig</returns>
        public ServerConfig GetServerConfig(SocketTextChannel channel)
        {
            ServerConfig _config = new();
            _xml = new XmlSerializer(typeof(ServerConfig));
            foreach(string path in Directory.GetFiles("./cache/serverconfig/"))
            {
                FileStream fs = new(path, FileMode.Open);
                _config = (ServerConfig)_xml.Deserialize(fs);
                fs.Close();
                if (_config.NewsChannelID == channel.Id)
                {
                    return _config;
                }
            }
            return null;
        }
        /// <summary>
        /// Gets the Serverconfig
        /// </summary>
        /// <param name="guild">Guild of wanted config</param>
        /// <returns>ServerConfig</returns>
        public ServerConfig GetServerConfig(SocketGuild guild)
        {
            ServerConfig _config = new();
            _xml = new XmlSerializer(typeof(ServerConfig));
            FileStream fs = new("./cache/serverconfig/" + guild.Id + ".xml", FileMode.Open);
            _config = (ServerConfig)_xml.Deserialize(fs);
            fs.Close();
            return _config;
        }
    }

}
