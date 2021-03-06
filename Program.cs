﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private ConfigClass Botconfig;

        public async Task RunBotAsync()
        {
            DiscordSocketConfig _config = new() { };
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            Botconfig = Config.LoadConfig();
            StatsUpdater.InitStats();

            string BotToken = Botconfig.BotToken;

            _client.Log += Log;
            _client.ReactionAdded += ReactionAdd;
            _client.JoinedGuild += GuildJoined;
            _client.LeftGuild += GuildLeft;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();

            await _client.SetGameAsync("!help");

            //await _scoreboard.ConnectWebSocket();
            //PlayerCard.PlayerTest();
            //catch if serverconfigs exist
            await Task.Delay(3000);
            foreach (SocketGuild guild in _client.Guilds)
            {
                await Config.GuildJoined(guild, null, true);
            }
#if RELEASE
            await BGTask();
#endif

            await Task.Delay(-1);
        }

        private Task GuildLeft(SocketGuild arg)
        {
            IMongoCollection<ServerConfig> collection = Config.GetCollection();
            collection.DeleteOne(x => x.GuildID == arg.Id);
            StatsUpdater.StatsTracker.Servercount = _client.Guilds.Count;
            StatsUpdater.UpdateStats();
            return Task.CompletedTask;
        }

        private async Task GuildJoined(SocketGuild guild)
        {
            StatsUpdater.StatsTracker.Servercount = _client.Guilds.Count;
            StatsUpdater.UpdateStats();
            await Config.GuildJoined(guild);
        }

        private async Task BGTask()
        {            
            int lastUpdate = 0;
            while (true)
            {
                //top.gg API & bots.gg API             
                if (DateTime.Now.Hour > lastUpdate && _client.CurrentUser.Id == 807182830752628766)
                {
                    lastUpdate = DateTime.Now.Hour;
                    HttpClient http = new();
                    //top.gg
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Botconfig.TopGGApiKey);
                    HttpRequestMessage req = new(HttpMethod.Post, "https://top.gg/api/bots/807182830752628766/stats");
                    req.Content = new StringContent($"{{ \"server_count\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                    await http.SendAsync(req);
                    //bots.gg
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Botconfig.BotsGGApiKey);
                    req = new(HttpMethod.Post, "https://discord.bots.gg/api/v1/bots/807182830752628766/stats");
                    req.Content = new StringContent($"{{ \"guildCount\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                    await http.SendAsync(req);
                }
                Stopwatch watch = new(); watch.Start();
                await HltvUpcomingAndLiveMatches.AktUpcomingAndLiveMatches();
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\tLiveAndUpcomingMatches aktualisiert ({watch.ElapsedMilliseconds}ms)");
                await Task.Delay(Botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                await HltvResults.AktResults(_client);
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\tResults aktualisiert ({watch.ElapsedMilliseconds}ms)"); 
                await Task.Delay(Botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                await HltvEvents.AktEvents(await Config.GetChannels(_client));
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\tEvents aktualisiert ({watch.ElapsedMilliseconds}ms)");
                await Task.Delay(Botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                await HltvNews.AktHLTVNews(await Config.GetChannels(_client));
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\tNews aktualisiert ({watch.ElapsedMilliseconds}ms)"); watch.Restart();
                CacheCleaner.Cleaner(_client);
                await Task.Delay(Botconfig.CheckResultsTimeInterval / 4);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task ReactionAdd(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var Handler = Task.Run(async () =>
            {
                string[] numberEmoteStrings = { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣" };
                GuildEmote emote = await Config.GetEmote(_client);
                if (emote.ToString() == reaction.Emote.ToString() || Array.IndexOf(numberEmoteStrings, reaction.Emote.ToString()) > -1)
                {
                    IUserMessage msg;
                    try { msg = await cacheable.GetOrDownloadAsync(); }
                    catch (Discord.Net.HttpException) { return; }
                    if (!msg.Author.IsBot || reaction.User.Value.IsBot) { return; }

                    if (Array.IndexOf(numberEmoteStrings, reaction.Emote.ToString()) > -1)
                    {
                        foreach (string emoteString in numberEmoteStrings)
                        {
                            if (emoteString == reaction.Emote.ToString()) { /*HltvLive.StartScoreboard(msg, new Emoji(reaction.Emote.ToString()), (channel as SocketGuildChannel).Guild); return;*/ }
                        }
                    }
                    else if (emote.ToString() == reaction.Emote.ToString())
                    {
                        IEmbed embedReac = null;
                        foreach (IEmbed em in msg.Embeds)
                        {
                            embedReac = em;
                        }

                        if (embedReac == null) { return; }
                        if (embedReac.Author == null) { return; }
                        if (embedReac.Author.Value.Name.ToString().ToLower() == "click here for more details")
                        {
                            await msg.RemoveAllReactionsAsync();
                            await HltvResults.SendPlStats(embedReac.Author.Value.Url, (ITextChannel)reaction.Channel);
                        }
                    }
                }
            });                      
        }

        public static void WriteLog(string arg)
        {
            Console.WriteLine(arg);
            if(!File.Exists($"./cache/log/{DateTime.Now.ToLongDateString()}.log")) { var fs = File.Create($"./cache/log/{DateTime.Now.ToLongDateString()}.log"); fs.Close(); }
            File.WriteAllText($"./cache/log/{DateTime.Now.ToLongDateString()}.log", File.ReadAllText($"./cache/log/{DateTime.Now.ToLongDateString()}.log") + "\n" + arg);
        }
        private Task Log(LogMessage arg)
        {
            WriteLog(arg.ToString().Split("     ")[0] + "\t" + arg.ToString().Split("     ")[1]);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task HandleCommandAsync(SocketMessage arg)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var Handler = Task.Run(async ()=>
            {
                SocketUserMessage Message = arg as SocketUserMessage;

                if (Message is null || Message.Author.IsBot)
                    return;

                int argPos = 0;
                string prefix;
                if (Message.Channel as SocketGuildChannel == null) { prefix = "!"; }
                else { prefix = Config.GetServerConfig((Message.Channel as SocketGuildChannel).Guild).Prefix; }

                if (Message.HasStringPrefix(prefix, ref argPos) || Message.HasStringPrefix($"{prefix} ", ref argPos) || Message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    SocketCommandContext context = new(_client, Message);
                    IResult Result = await _commands.ExecuteAsync(context, argPos, _services);

                    //Log Commands
                    FileStream fs = File.OpenWrite($"./cache/log/{DateTime.Now.ToShortDateString()}.txt"); fs.Close();
                    string ori = File.ReadAllText($"./cache/log/{DateTime.Now.ToShortDateString()}.txt");
                    File.WriteAllText($"./cache/log/{DateTime.Now.ToShortDateString()}.txt", ori + DateTime.Now.ToShortTimeString() + " " + Message.Channel.ToString() + " " + Message.ToString() + "\n");

                    if (!Result.IsSuccess)
                        WriteLog(Result.ErrorReason);
                    else
                    {
                        StatsUpdater.StatsTracker.Commands += 1;
                        StatsUpdater.UpdateStats();
                    }
                }                
            });
        }
    }
}
