using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge
{
    internal class Program
    {
        private static void Main()
        {
            GetInstance().RunBotAsync().GetAwaiter().GetResult();
        }

        private static Program _instance;
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private ConfigClass _botconfig;
        public readonly HttpClient DefaultHttpClient;
        SlashCommands _commands;

        private Program()
        {
            DefaultHttpClient = new HttpClient();
        }

        public static Program GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Program();
            }

            return _instance;
        }
        
        public async Task RunBotAsync()
        {
            DiscordSocketConfig _config = new() { GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites };
            _client = new DiscordSocketClient(_config);
            _commands = new SlashCommands(_client);

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .BuildServiceProvider();

            _botconfig = Config.LoadConfig();
            //StatsUpdater.InitStats();

            string botToken = _botconfig.BotToken;

            _client.Log += Log;
            _client.JoinedGuild += GuildJoined;
            _client.LeftGuild += GuildLeft;
            _client.ButtonExecuted += ButtonExecuted;
            _client.Ready += Ready;
            _client.SlashCommandExecuted += _commands.SlashCommandHandler;
            _client.SelectMenuExecuted += SelectMenuExecuted;
            

            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();
            await _client.SetGameAsync("/help");
            await Task.Delay(-1);
        }

        private static Task SelectMenuExecuted(SocketMessageComponent arg)
        {
            Task handler = Task.Run(async () =>
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
            return handler;
        }

        private async Task Ready()
        {
            await Config.ServerConfigStartUp(_client);
            // Config.InitAllWebhooks(_client);
            await BgTask();
        }

        private Task ButtonExecuted(SocketMessageComponent arg)
        {
            Task handler = Task.Run(async () =>
            {
                string matchLink = "";
                Match match;
                MatchMapStats mapStats;
                MatchStats matchStats;
                switch (arg.Data.CustomId)
                {
                    case "overallstats_bo1":
                        await arg.DeferAsync();
                        foreach (Embed e in arg.Message.Embeds)
                        {
                            matchLink = ((EmbedAuthor)e.Author).Url;
                        }
                        match = await HltvMatch.GetMatch(matchLink);
                        mapStats = await HltvMatchMapStats.GetMatchMapStats(match.maps[0]);
                        await arg.Channel.SendMessageAsync(embed: HltvMatchStats.GetPlayerStatsEmbed(mapStats));
                        break;
                    case "overallstats_def":
                        await arg.DeferAsync();
                        
                        foreach (Embed e in arg.Message.Embeds)
                        {
                            matchLink = ((EmbedAuthor)e.Author).Url;
                        }
                        match = await HltvMatch.GetMatch(matchLink);
                        matchStats = await HltvMatchStats.GetMatchStats(match);
                        await arg.Channel.SendMessageAsync(embed: HltvMatchStats.GetPlayerStatsEmbed(matchStats));
                        break;
                }
            });
            return handler;
        }

        private Task GuildLeft(SocketGuild arg)
        {
            IMongoCollection<ServerConfig> collection = Config.GetCollection();
            collection.DeleteOne(x => x.GuildID == arg.Id);
            StatsUpdater.StatsTracker.Servercount = _client.Guilds.Count;
            StatsUpdater.UpdateStats();
            return Task.CompletedTask;
        }

        public async Task GuildJoined(SocketGuild guild)
        {
            StatsUpdater.StatsTracker.Servercount = _client.Guilds.Count;
            StatsUpdater.UpdateStats();
            await Config.GuildJoined(guild);
        }

        private Task BgTask()
        {
            return Task.Run(async() =>
            {                
                int lastUpdate = 0;
                while (true)
                {
                    //top.gg API & bots.gg API
                    if (DateTime.Now.Hour > lastUpdate && _client.CurrentUser.Id == 807182830752628766)
                    {
                            lastUpdate = DateTime.Now.Hour;
                            //top.gg
                            DefaultHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botconfig.TopGGApiKey);
                            HttpRequestMessage req = new(HttpMethod.Post, "https://top.gg/api/bots/807182830752628766/stats");
                            req.Content = new StringContent($"{{ \"server_count\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                            await DefaultHttpClient.SendAsync(req);
                            //bots.gg
                            DefaultHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botconfig.BotsGGApiKey);
                            req = new(HttpMethod.Post, "https://discord.bots.gg/api/v1/bots/807182830752628766/stats");
                            req.Content = new StringContent($"{{ \"guildCount\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                            await DefaultHttpClient.SendAsync(req);
                    }

                    try
                    {
                        Stopwatch watch = new(); watch.Start();
                        await HltvResults.SendNewResults();
                        WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched results ({watch.ElapsedMilliseconds}ms)");
                        await Task.Delay(_botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                        await HltvEvents.AktEvents(await Config.GetChannelsLegacy(_client));
                        WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched events ({watch.ElapsedMilliseconds}ms)");
                        await Task.Delay(_botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                        await HltvNews.SendNewNews(await Config.GetChannelsLegacy(_client));
                        WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched news ({watch.ElapsedMilliseconds}ms)"); watch.Restart();
                        CacheCleaner.Cleaner(_client);
                        await Task.Delay(_botconfig.CheckResultsTimeInterval / 4);
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        await Task.Delay(_botconfig.CheckResultsTimeInterval / 4);
                    }
                }
            });
        }

        public static void WriteLog(string arg)
        {
            Console.WriteLine(arg);
        }
        private static Task Log(LogMessage arg)
        {
            WriteLog(arg.ToString().Split("     ")[0] + "\t" + arg.ToString().Split("     ")[1]);
            return Task.CompletedTask;
        }
    }
}
