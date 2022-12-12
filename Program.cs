using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace HLTVDiscordBridge
{
    internal class Program
    {
        private static async Task Main()
        {
            await GetInstance().Start();
            await Task.Delay(-1);
        }

        private static Program _instance;
        private readonly DiscordSocketClient _client;
        private IServiceProvider _services;
        private readonly BotConfig _botConfig;
        public readonly HttpClient DefaultHttpClient;

        private Program()
        {
            DefaultHttpClient = new HttpClient();
            _client = new DiscordSocketClient( new DiscordSocketConfig
                { GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites });
            SlashCommands commands = new(_client);
            _services = new ServiceCollection().AddSingleton(_client).BuildServiceProvider();
            _botConfig = BotConfig.GetBotConfig();
            
            _client.Log += Log;
            _client.JoinedGuild += GuildJoined;
            _client.LeftGuild += GuildLeft;
            _client.ButtonExecuted += ButtonExecuted;
            _client.Ready += Ready;
            _client.SlashCommandExecuted += commands.SlashCommandHandler;
            _client.SelectMenuExecuted += SelectMenuExecuted;
        }

        public static Program GetInstance()
        {
            return _instance ??= new Program();
        }
        
        private async Task Start()
        {
            await _client.LoginAsync(TokenType.Bot, _botConfig.BotToken);
            await _client.StartAsync();
            await _client.SetGameAsync("/help");
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
            await BgTask();
        }

        private static Task ButtonExecuted(SocketMessageComponent arg)
        {
            Task handler = Task.Run(async () =>
            {
                string matchLink = "";
                Match match;
                switch (arg.Data.CustomId)
                {
                    case "overallstats_bo1":
                        await arg.DeferAsync();
                        foreach (Embed e in arg.Message.Embeds)
                        {
                            matchLink = ((EmbedAuthor)e.Author).Url;
                        }
                        match = await HltvMatch.GetMatch(matchLink);
                        MatchMapStats mapStats = await HltvMatchMapStats.GetMatchMapStats(match.maps[0]);
                        await arg.Channel.SendMessageAsync(embed: HltvMatchStats.GetPlayerStatsEmbed(mapStats));
                        break;
                    case "overallstats_def":
                        await arg.DeferAsync();
                        
                        foreach (Embed e in arg.Message.Embeds)
                        {
                            matchLink = ((EmbedAuthor)e.Author).Url;
                        }
                        match = await HltvMatch.GetMatch(matchLink);
                        MatchStats matchStats = await HltvMatchStats.GetMatchStats(match);
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

        private async Task UpdateGgServerStats()
        {   //top.gg
            HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botConfig.TopGgApiKey);
            HttpRequestMessage reqTop = new(HttpMethod.Post, $"https://top.gg/api/bots/${_client.CurrentUser.Id}/stats");
            reqTop.Content = new StringContent($"{{ \"server_count\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
            await DefaultHttpClient.SendAsync(reqTop);
            
            //bots.gg
            DefaultHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botConfig.BotsGgApiKey);
            HttpRequestMessage reqBots = new (HttpMethod.Post, $"https://discord.bots.gg/api/v1/bots/${_client.Guilds.Count}/stats");
            reqBots.Content = new StringContent($"{{ \"guildCount\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
            await DefaultHttpClient.SendAsync(reqBots);
        }

        private async Task BgTask()
        {
            if (_client.CurrentUser.Id == _botConfig.ProductionBotId)
            {
                Timer updateGgStatsTimer = new(1000 * 60 * 60);
                updateGgStatsTimer.Elapsed += async (sender, e) => await UpdateGgServerStats();
                updateGgStatsTimer.Enabled = true;
            }
            (Timer, Func<Task>)[] timers = { (new Timer(), HltvResults.SendNewResults), (new Timer(), HltvEvents.AktEvents), (new Timer(), HltvNews.SendNewNews) };
            await HltvNews.SendNewNews();
            foreach ((Timer timer, Func<Task> function) in timers)
            {
                try
                {
                    await function();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                timer.Interval = _botConfig.CheckResultsTimeInterval;
                timer.Elapsed += async (s, e) => await function();
                timer.Enabled = true;
                await Task.Delay(_botConfig.CheckResultsTimeInterval / timers.Length);
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
