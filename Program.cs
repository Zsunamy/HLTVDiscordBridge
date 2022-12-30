﻿using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge;

internal class Program
{
    private static async Task Main()
    {
        await GetInstance().Start();
        await Task.Delay(-1);
    }

    private static Program _instance;
    private readonly DiscordSocketClient _client;

    private readonly BotConfig _botConfig;
    public static HttpClient DefaultHttpClient { get; } = new();
    private Task _bgTask;
    public static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private Program()
    {
        _client = new DiscordSocketClient( new DiscordSocketConfig
            { GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites });
        SlashCommands commands = new(_client);
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
        if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "init")
        {
            WriteLog($"{DateTime.Now.ToLongTimeString()} Init\t\t successfully initialized all commands");
            await new SlashCommands(_client).InitSlashCommands();
        }
        
        await Config.ServerConfigStartUp(_client);
        _bgTask ??= BgTask();
    }

    private static Task ButtonExecuted(SocketMessageComponent arg)
    {
        Task handler = Task.Run(async () =>
        {
            string matchLink = "";
            await arg.DeferAsync();
            foreach (Embed e in arg.Message.Embeds)
            {
                matchLink = ((EmbedAuthor)e.Author!).Url;
            }
            GetMatch request = new GetMatch{ Id = Tools.GetIdFromUrl(matchLink)};
            Match match = await request.SendRequest<Match>();

            if (arg.Data.CustomId == "overallstats_bo1")
            {
                GetMatchMapStats requestMapStats = new GetMatchMapStats{Id = match.Maps[0].StatsId};
                await arg.Channel.SendMessageAsync(embed: (await requestMapStats.SendRequest<MatchMapStats>()).ToEmbed());
            }
            else
            {
                GetMatchStats requestMatchStats = new GetMatchStats{Id = match.StatsId};
                await arg.Channel.SendMessageAsync(embed: (await requestMatchStats.SendRequest<MatchStats>()).ToEmbed());
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
    {
        if (_client.CurrentUser.Id == _botConfig.ProductionBotId)
        {
            //top.gg
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

        //CacheCleaner
        CacheCleaner.Clean();
        
        //update ranking
        await HltvRanking.UpdateTeamRanking();
    }

    private async Task BgTask()
    {
        await UpdateGgServerStats();
        Timer updateGgStatsTimer = new(1000 * 60 * 60);
        updateGgStatsTimer.Elapsed += async (sender, e) =>
        {
            try
            {
                await UpdateGgServerStats();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        };
        updateGgStatsTimer.Enabled = true;
        
        (Timer, Func<Task>)[] timers = {(new Timer(), HltvNews.SendNewNews), (new Timer(), HltvResults.SendNewResults),
            (new Timer(), HltvEvents.SendNewStartedEvents), (new Timer(), HltvEvents.SendNewPastEvents), (new Timer(), HltvMatches.UpdateMatches)};
        foreach ((Timer timer, Func<Task> function) in timers)
        {
            await function();
            timer.Interval = _botConfig.CheckResultsTimeInterval;
            timer.Elapsed += async (s, e) =>
            {
                try
                {
                    await function();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            };
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