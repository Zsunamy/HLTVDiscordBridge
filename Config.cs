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
    }

    public class ServerConfig
    {
        public ulong guildID { get; set; }
        public ulong NewsChannelID { get; set; }
        public ushort MinimumStars { get; set; }
        public ulong EmoteID { get; set; }
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
                    .WithDescription("Please mind the syntax: !minstars [stars (number between 0-5)]")
                    .WithCurrentTimestamp();
                await ReplyAsync("", false, builder.Build());
            }            
            ServerConfig _config = new ServerConfig();
            _config = GetServerConfig(Context.Guild);
            _config.MinimumStars = starsNum;
            FileStream fs = new FileStream("./cache/serverconfig/" + Context.Guild.Id + ".xml", FileMode.Open);
            XmlSerializer _xml = new XmlSerializer(typeof(ServerConfig));
            _xml.Serialize(fs, _config);
            fs.Close();
            builder.WithColor(Color.Green)
                    .WithTitle("SUCCESS")
                    .WithDescription($"You successfully changed the minimum stars to output a HLTV match to \"{starsNum}\"")
                    .WithCurrentTimestamp();
            await ReplyAsync("", false, builder.Build());
        }

        /// <summary>
        /// Creates channel and sets it as output for HLTVNews and HLTVMatches
        /// </summary>
        /// <param name="guild">Guild on which the Channel should be created</param>
        /// <param name="channelID">Channel ID (default 0 if a channel should be created)</param>
        /// <param name="channelname">Sets a custom Channelname</param>
        public async Task GuildJoined(SocketGuild guild, SocketTextChannel channel = null)
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
            try { _config.EmoteID = (await guild.CreateEmoteAsync("hltvstats", new Image("./res/headshot.png"))).Id; }
            catch(Discord.Net.HttpException)
            {
                builder.WithTitle("INIT ERROR")
                    .WithDescription("Please make sure that the HLTV bot has enough permission and that there is at least one custom emoji slot left. Try to add the bot again!");
                var PM = await guild.Owner.GetOrCreateDMChannelAsync();
                await PM.SendMessageAsync("", false, builder.Build());
                await guild.LeaveAsync();
                return;
            }
            

            _xml = new XmlSerializer(typeof(ServerConfig));
            Directory.CreateDirectory("./cache/serverconfig");
            FileStream stream = new FileStream($"./cache/serverconfig/{guild.Id}.xml", FileMode.OpenOrCreate);  

            _xml.Serialize(stream, _config);            
            stream.Close();
            try { await channel.SendMessageAsync("", false, builder.Build()); }
            catch(Discord.Net.HttpException)
            {
                builder.WithDescription($"Thanks for adding the HLTVDiscordBridge to {guild.Name}. To set a default HLTV-News output channel, type !init " +
                    $"in a channel of your choice, but make sure that the bot has enough permission to access and send messages in that channel. " +
                    $"Type !help for more info about how to proceed. If there are any questions or issues feel free to contact us!\n" +
                    $"https://github.com/Zsunamy/HLTVDiscordBridge/issues \n<@248110264610848778>\n<@224037892387766272>\n<@255000770707980289>");
                await guild.Owner.SendMessageAsync("", false, builder.Build());
            }
               
        }

        /// <summary>
        /// Gets all HLTV output channels of the client
        /// </summary>
        /// <param name="client">acting client</param>
        /// <returns>List<SocketTextChannel> of all channels</returns>
        public List<SocketTextChannel> GetChannels (DiscordSocketClient client)
        {
            List<SocketTextChannel> channel = new List<SocketTextChannel>();
            ServerConfig _config = new ServerConfig();
            _xml = new XmlSerializer(typeof(ServerConfig));
            foreach (SocketGuild guild in client.Guilds)
            {
                FileStream fs = new FileStream($"./cache/serverconfig/{guild.Id}.xml", FileMode.Open);
                _config = (ServerConfig)_xml.Deserialize(fs);
                fs.Close();
                channel.Add((SocketTextChannel)client.GetChannel(_config.NewsChannelID));
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
