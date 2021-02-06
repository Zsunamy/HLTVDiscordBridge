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
        public async Task InitTextChannel(string name = "default")
        {
            if (name == "default")
            {
                await GuildJoined(Context.Guild, Context.Channel.Id);
            } else
            {
                await GuildJoined(Context.Guild, 0, name);
            }         
        }

        /// <summary>
        /// Creates channel and sets it as output for HLTVNews and HLTVMatches
        /// </summary>
        /// <param name="guild">Guild on which the Channel should be created</param>
        /// <param name="channelID">Channel ID (default 0 if a channel should be created)</param>
        /// <param name="channelname">Sets a custom Channelname</param>
        public async Task GuildJoined(SocketGuild guild, ulong channelID = 0, string channelname = "hltv-news-feed")
        {
            ServerConfig _config = new ServerConfig();
            if (channelID == 0)
            {
                RestTextChannel channel = await guild.CreateTextChannelAsync(channelname);
                channelID = channel.Id;
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("Init")
                    .WithDescription($"Success! You created the channel {channel.Mention} and set it as default output for HLTV-NEWS")
                    .WithCurrentTimestamp()
                    .WithColor(Color.DarkBlue);
                await channel.SendMessageAsync("", false, builder.Build());
            }
            else
            {
                SocketTextChannel channel;
                channel = guild.GetTextChannel(channelID);
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("Init")
                    .WithDescription($"Success! You are now using the channel {channel.Mention} as default output for HLTV-NEWS")
                    .WithCurrentTimestamp()
                    .WithColor(Color.DarkBlue);
                await channel.SendMessageAsync("", false, builder.Build());
            }

            _config.NewsChannelID = channelID;
            _config.guildID = guild.Id;
            _config.MinimumStars = 0;

            _xml = new XmlSerializer(typeof(ServerConfig));
            Directory.CreateDirectory("./cache/serverconfig");
            FileStream stream = new FileStream($"./cache/serverconfig/{guild.Id}.xml", FileMode.OpenOrCreate);  

            _xml.Serialize(stream, _config);            
            stream.Close();
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
    }

}
