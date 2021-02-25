using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HLTVDiscordBridge
{
    public class ConfigClass
    {
        public string BotToken { get; set; }
        public int CheckResultsTimeInterval { get; set; }
        public string topGGApiKey { get; set; }
    }

    public class ServerConfig
    {
        public ulong guildID { get; set; }
        public ulong NewsChannelID { get; set; }
        public ushort MinimumStars { get; set; }
        public bool OnlyFeaturedEvents { get; set; }
        public string Prefix { get; set; }
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
        [Command("init"), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task InitTextChannel(SocketTextChannel channel = null)
        {
            if(channel == null)
            {
                channel = (SocketTextChannel)Context.Channel;
            }
            await GuildJoined(Context.Guild, channel);
        }

        [Command("minstars"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ChangeMinStars(string stars = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            ushort starsNum;
            if(!ushort.TryParse(stars, out starsNum) || stars == "" || starsNum < 0 || starsNum > 5)
            {
                builder.WithColor(Color.Red)
                    .WithTitle("SYNTAX ERROR")
                    .WithDescription($"Please mind the syntax: {GetServerConfig(Context.Guild).Prefix}minstars [stars (number between 0-5)]")
                    .WithCurrentTimestamp();
                await ReplyAsync("", false, builder.Build());
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
                    .WithCurrentTimestamp();
            await ReplyAsync("", false, builder.Build());
        }

        [Command("featuredevents"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ChangeFeaturedEvents(string arg = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            ServerConfig _config = new ServerConfig();
            _config = GetServerConfig(Context.Guild);
            string description = "You successfully changed the event output ";
            if (arg.ToLower() != "true" && arg.ToLower() != "false")
            {
                builder.WithColor(Color.Red)
                    .WithTitle("SYNTAX ERROR")
                    .WithDescription($"Please mind the syntax: {_config.Prefix}featuredevents [true/false]")
                    .WithCurrentTimestamp();
                await ReplyAsync("", false, builder.Build());
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
                    .WithCurrentTimestamp();
            await ReplyAsync("", false, builder.Build());
        }
        [Command("prefix"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ChangePrefix(string arg = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            if (arg == "") {
                builder.WithColor(Color.Red)
                        .WithTitle("SYNTAX ERROR")
                        .WithDescription($"Please mind the syntax: \"{GetServerConfig(Context.Guild).Prefix}prefix [Prefix]\"")
                        .WithCurrentTimestamp();
                await ReplyAsync("", false, builder.Build());
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
                    .WithCurrentTimestamp();
            await ReplyAsync("", false, builder.Build());
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
                builder.WithTitle("INIT")
                    .WithDescription($"Thanks for adding the HLTVDiscordBridge to {guild.Name}. {channel.Mention} is set as default output for HLTV-NEWS. " +
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

            _config.NewsChannelID = channel.Id;
            _config.guildID = guild.Id;
            _config.MinimumStars = 0;
            _config.OnlyFeaturedEvents = false;
            _config.Prefix = "!";

            _xml = new XmlSerializer(typeof(ServerConfig));
            Directory.CreateDirectory("./cache/serverconfig");
            if(File.Exists($"./cache/serverconfig/{guild.Id}.xml") && startup) { return; }
            FileStream stream = new FileStream($"./cache/serverconfig/{guild.Id}.xml", FileMode.Create);  
            _xml.Serialize(stream, _config);            
            stream.Close();
            try { await channel.SendMessageAsync("", false, builder.Build()); }
            catch(Discord.Net.HttpException)
            {
                builder.WithDescription($"Thanks for adding the HLTVDiscordBridge to {guild.Name}. To set a default HLTV-News output channel, type !init " +
                    $"in a channel of your choice, but make sure that the bot has enough permission to access and send messages in that channel. " +
                    $"Type !help for more info about how to proceed. If there are any questions or issues feel free to contact us!\n" +
                    $"https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>");
                try { await guild.Owner.SendMessageAsync("", false, builder.Build()); }
                catch (Discord.Net.HttpException) { }
            }
               
        }

        public async Task<GuildEmote> GetEmote(DiscordSocketClient client)
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
