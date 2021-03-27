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
    }

    public class Config : ModuleBase<SocketCommandContext>
    {

        XmlSerializer _xml;
        /// <summary>
        /// Loads the generel bot config
        /// </summary>
        /// <returns>Config</returns>
        public ConfigClass LoadConfig()
        {
            ConfigClass conf = new ConfigClass();
            _xml = new XmlSerializer(typeof(ConfigClass));
            FileStream stream = new FileStream("./config.xml", FileMode.Open);
            conf = (ConfigClass)_xml.Deserialize(stream);
            stream.Close();
            return conf;
        }

        #region Commands
        [Command("init")]
        public async Task InitTextChannel(SocketTextChannel channel = null)
        {
            EmbedBuilder builder = new EmbedBuilder();
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel)))
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                builder.WithTitle("ERROR")
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

        [Command("minstars")]
        public async Task ChangeMinStars(string stars = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel))) 
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the minimum stars!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (!ushort.TryParse(stars, out ushort starsNum) || stars == "" || starsNum < 0 || starsNum > 5)
            {
                builder.WithColor(Color.Red)
                    .WithTitle("SYNTAX ERROR")
                    .WithDescription($"Please mind the syntax: {GetServerConfig(Context.Guild).Prefix}minstars [stars (number between 0-5)]")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            ServerConfig _config = new ServerConfig();
            _config = GetServerConfig(Context.Guild);
            _config.MinimumStars = starsNum;
            FileStream fs = new FileStream("./cache/serverconfig/" + Context.Guild.Id + ".xml", FileMode.Create);
            XmlSerializer _xml = new XmlSerializer(typeof(ServerConfig));
            _xml.Serialize(fs, _config);
            fs.Close();
            builder.WithColor(Color.Green)
                    .WithTitle("SUCCESS")
                    .WithDescription($"You successfully changed the minimum stars to output a HLTV match to \"{starsNum}\"")
                    .WithCurrentTimestamp()
                    .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
        }

        [Command("featuredevents")]
        public async Task ChangeFeaturedEvents(string arg = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            ServerConfig _config = new ServerConfig();
            _config = GetServerConfig(Context.Guild);
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel)))
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the featured events!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            string description = "You successfully changed the event output to ";
            if (arg.ToLower() != "true" && arg.ToLower() != "false")
            {
                builder.WithColor(Color.Red)
                    .WithTitle("SYNTAX ERROR")
                    .WithDescription($"Please mind the syntax: {_config.Prefix}featuredevents [true/false]")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
            }
            else if (arg.ToLower() == "true") { _config.OnlyFeaturedEvents = true; description += "ONLY FEATURED EVENTS"; }
            else { _config.OnlyFeaturedEvents = false; description += "SHOW ALL EVENTS"; }

            FileStream fs = new FileStream("./cache/serverconfig/" + Context.Guild.Id + ".xml", FileMode.Create);
            XmlSerializer _xml = new XmlSerializer(typeof(ServerConfig));
            _xml.Serialize(fs, _config);
            fs.Close();
            builder.WithColor(Color.Green)
                    .WithTitle("SUCCESS")
                    .WithDescription(description)
                    .WithCurrentTimestamp()
                    .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
        }

        [Command("prefix")]
        public async Task ChangePrefix(string arg = "")
        {            
            EmbedBuilder builder = new EmbedBuilder();
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel)))
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the prefix!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (arg == "") {
                builder.WithColor(Color.Green)
                        .WithTitle("PREFIX")
                        .WithDescription($"Your current prefix is: \"{GetServerConfig(Context.Guild).Prefix}\"")
                        .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            } 
            
            ServerConfig _config = new ServerConfig();
            _config = GetServerConfig(Context.Guild);
            _config.Prefix = arg;

            FileStream fs = new FileStream("./cache/serverconfig/" + Context.Guild.Id + ".xml", FileMode.Create);
            XmlSerializer _xml = new XmlSerializer(typeof(ServerConfig));
            _xml.Serialize(fs, _config);
            fs.Close();
            builder.WithColor(Color.Green)
                    .WithTitle("SUCCESS")
                    .WithDescription($"You successfully changed the command prefix to \"{arg}\"")
                    .WithCurrentTimestamp()
                    .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
        }
        public async Task ChangeResultOutput(string arg = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            ServerConfig cfg = GetServerConfig(Context.Guild);
            string state = GetServerConfig(Context.Guild).ResultOutput ? "disabled" : "enabled";
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel)))
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("Please use this command only on guilds!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (!(Context.User as SocketGuildUser).GuildPermissions.Administrator)
            {
                builder.WithTitle("ERROR")
                    .WithColor(Color.Red)
                    .WithDescription("You do not have enough permission to change the ResultOutput!")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            if (arg == "")
            {                
                builder.WithColor(Color.Green)
                        .WithTitle("PREFIX")
                        .WithDescription($"The automated ResultOutput is: \"{state}\"")
                        .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            } else if(bool.TryParse(arg, out bool res)) { cfg.ResultOutput = res; }
            else {
                builder.WithTitle("SYNTAX ERROR")
                 .WithColor(Color.Red)
                 .WithDescription($"Please mind the syntax: {GetServerConfig(Context.Guild).Prefix}ResultOutput [true / false]")
                 .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                return;
            }
            FileStream fs = new FileStream("./cache/serverconfig/" + Context.Guild.Id + ".xml", FileMode.Create);
            XmlSerializer _xml = new XmlSerializer(typeof(ServerConfig));
            _xml.Serialize(fs, cfg);
            fs.Close();
            builder.WithColor(Color.Green)
                    .WithTitle("SUCCESS")
                    .WithDescription($"You successfully changed the automated result output to: {state}")
                    .WithCurrentTimestamp()
                    .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
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
            EmbedBuilder builder = new EmbedBuilder();
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
            ServerConfig _config = new ServerConfig();

            if(channel != null) { _config.NewsChannelID = channel.Id; }            
            _config.GuildID = guild.Id;
            _config.MinimumStars = 0;
            _config.OnlyFeaturedEvents = false;
            _config.Prefix = "!";

            _xml = new XmlSerializer(typeof(ServerConfig));
            Directory.CreateDirectory("./cache/serverconfig");
            if(File.Exists($"./cache/serverconfig/{guild.Id}.xml") && startup) { return; }
            FileStream stream = new FileStream($"./cache/serverconfig/{guild.Id}.xml", FileMode.Create);  
            _xml.Serialize(stream, _config);            
            stream.Close();
            try { await channel.SendMessageAsync(embed: builder.Build()); }
            catch(Discord.Net.HttpException)
            {
                builder.WithDescription($"Thanks for adding the HLTVDiscordBridge to {guild.Name}. To set a default HLTV-News output channel, type !init " +
                    $"in a channel of your choice, but make sure that the bot has enough permission to access and send messages in that channel. " +
                    $"Type !help for more info about how to proceed. If there are any questions or issues feel free to contact us!\n" +
                    $"https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>");
                try { await guild.Owner.SendMessageAsync(embed: builder.Build()); }
                catch (Discord.Net.HttpException) { }
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

        /// <summary>
        /// Gets all HLTV output channels of the client
        /// </summary>
        /// <param name="client">acting client</param>
        /// <returns>List<SocketTextChannel> of all channels</returns>
        public async Task<List<SocketTextChannel>> GetChannels (DiscordSocketClient client)
        {
            List<SocketTextChannel> channel = new List<SocketTextChannel>();
            ServerConfig _config = new ServerConfig();
            _xml = new XmlSerializer(typeof(ServerConfig));
            foreach (SocketGuild guild in client.Guilds)
            {
                if (!File.Exists($"./cache/serverconfig/{guild.Id}.xml")) { await GuildJoined(guild); }                
                FileStream fs = new FileStream($"./cache/serverconfig/{guild.Id}.xml", FileMode.Open);
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
            ServerConfig _config = new ServerConfig();
            _xml = new XmlSerializer(typeof(ServerConfig));
            foreach(string path in Directory.GetFiles("./cache/serverconfig/"))
            {
                FileStream fs = new FileStream(path, FileMode.Open);
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
            ServerConfig _config = new ServerConfig();
            _xml = new XmlSerializer(typeof(ServerConfig));
            FileStream fs = new FileStream("./cache/serverconfig/" + guild.Id + ".xml", FileMode.Open);
            _config = (ServerConfig)_xml.Deserialize(fs);
            fs.Close();
            return _config;
        }
    }

}
