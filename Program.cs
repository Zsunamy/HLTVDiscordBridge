using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            DiscordSocketConfig _config = new() { GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites };
            _client = new DiscordSocketClient(_config);
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            Botconfig = Config.LoadConfig();
            //StatsUpdater.InitStats();

            string BotToken = Botconfig.BotToken;

            _client.Log += Log;
            _client.JoinedGuild += GuildJoined;
            _client.LeftGuild += GuildLeft;
            _client.ButtonExecuted += ButtonExecuted;
            _client.Ready += Ready;
            _client.SlashCommandExecuted += SlashCommands.SlashCommandHandler;
            _client.SelectMenuExecuted += SelectMenuExecuted;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();
            await _client.SetGameAsync("!help");

            //catch if serverconfigs exist
            /*await Task.Delay(3000);
            foreach (SocketGuild guild in _client.Guilds)
            {
                await Config.GuildJoined(guild, null, true);
            }*/
            //await HltvMatchStats.GetMatchStats(2353876);
            //await HltvResults.SendNewResults(_client);

            //await Test.test();

            //return;
#if RELEASE
            await BGTask();
#endif      
            
            await Task.Delay(-1);
        }

        private Task SelectMenuExecuted(SocketMessageComponent arg)
        {
            var Handler = Task.Run(async () =>
            {
                switch (arg.Data.CustomId)
                {
                    case "upcomingEventsMenu":
                        await HltvEvents.SendEvent(arg);
                        break;
                    case "ongoingEventsMenu":
                        await HltvEvents.SendEvent(arg);
                        break;
                }
            });
            return Handler;
        }

        private async Task Ready()
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                await Config.GuildJoined(guild, null, true);
            }            

            await SlashCommands.InitSlashCommands(_client);



            //await HltvEvents_new.GetUpcomingEvents();
            //wait HltvEvents_new.GetOngoingEvents();
            //await HltvEvents_new.GetStartedEvents();
            //await HltvEvents_new.GetPastEvents();
            //await HltvEvents_new.GetEndedEvents();
            //var user = await _client.GetUserAsync(248110264610848778);
            //await user.SendMessageAsync(embed: HltvEvents_new.GetEventStartedEmbed(await HltvEvents_new.GetFullEvent(6341)));
            //await HltvEvents_new.SendUpcomingEvents();            
        }

        private Task ButtonExecuted(SocketMessageComponent arg)
        {
            var Handler = Task.Run(async () =>
            {
                switch (arg.Data.CustomId)
                {
                    case "playerstats":
                        string matchLink = "";
                        foreach (Embed e in arg.Message.Embeds)
                        {
                            matchLink = ((EmbedAuthor)e.Author).Url;
                        }
                        Match match = await HltvMatch.GetMatch(matchLink);
                        MatchStats stats = await HltvMatchStats.GetMatchStats(match);
                        await arg.RespondAsync(embed: HltvMatchStats.GetPlayerStatsEmbed(stats));
                        break;
                }
            });
            return Handler;
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
                try
                {
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
                }
                catch(Exception ex)
                {
                    Console.Write(ex.ToString());
                }
                
                Stopwatch watch = new(); watch.Start();
                await HltvUpcomingAndLiveMatches.AktUpcomingAndLiveMatches();
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\tLiveAndUpcomingMatches aktualisiert ({watch.ElapsedMilliseconds}ms)");
                await Task.Delay(Botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                await HltvResults.SendNewResults(_client);
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

        public static void WriteLog(string arg)
        {
            Console.WriteLine(arg);
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
        private Task HandleCommandAsync(SocketMessage arg)
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
            return Handler;
        }
    }
}
