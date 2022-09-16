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
using System.Threading;
using System.Threading.Tasks;

namespace HLTVDiscordBridge
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private ConfigClass _botconfig;
        SlashCommands _commands;

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
            var handler = Task.Run(async () =>
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

        private Task Ready()
        {
            return Task.Run(async () =>
            {
                await Config.ServerconfigStartUp(_client);
                //await _commands.InitSlashCommands();
                Task.Run(() => BgTask());

            });          
        }

        private Task ButtonExecuted(SocketMessageComponent arg)
        {
            var handler = Task.Run(async () =>
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

        private async Task GuildJoined(SocketGuild guild)
        {
            StatsUpdater.StatsTracker.Servercount = _client.Guilds.Count;
            StatsUpdater.UpdateStats();
            await Config.GuildJoined(guild);
        }

        private async Task BgTask()
        {
            int lastUpdate = 0;
            while (true)
            {
                WriteLog("Restarting timeInterval");
                //top.gg API & bots.gg API
                try
                {
                    if (DateTime.Now.Hour > lastUpdate && _client.CurrentUser.Id == 807182830752628766)
                    {
                        lastUpdate = DateTime.Now.Hour;
                        HttpClient http = new();
                        //top.gg
                        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botconfig.TopGGApiKey);
                        HttpRequestMessage req = new(HttpMethod.Post, "https://top.gg/api/bots/807182830752628766/stats");
                        req.Content = new StringContent($"{{ \"server_count\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                        await http.SendAsync(req);
                        //bots.gg
                        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botconfig.BotsGGApiKey);
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
                await HltvResults.SendNewResults(_client);
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched results ({watch.ElapsedMilliseconds}ms)");
                WriteLog("waiting after results");
                await Task.Delay(_botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                WriteLog("done waiting after results");
                await HltvEvents.AktEvents(await Config.GetChannels(_client););
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched events ({watch.ElapsedMilliseconds}ms)");
                WriteLog("waiting after events");
                await Task.Delay(_botconfig.CheckResultsTimeInterval / 4); watch.Restart();
                WriteLog("done waiting after events");
                var newsChannels = await Config.GetChannels(_client);
                WriteLog("got all channels for news");
                await HltvNews.AktHLTVNews(newsChannels);
                WriteLog("waiting after news");
                WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched news ({watch.ElapsedMilliseconds}ms)"); watch.Restart();
                WriteLog("done waiting after news");
                CacheCleaner.Cleaner(_client);
                await Task.Delay(_botconfig.CheckResultsTimeInterval / 4);
            }
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
