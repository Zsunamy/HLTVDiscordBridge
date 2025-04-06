using Discord;
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
using Discord.Net;
using HLTVDiscordBridge.Repository;
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
    public DiscordSocketClient Client { get; }

    private readonly BotConfig _botConfig;
    public static HttpClient DefaultHttpClient { get; } = new();
    private Task _bgTask;
    
    //TODO after updating to v4 this needs to be deleted since it's required for the webhook migration.
    public static MongoClient DbClient { get; } = new(BotConfig.GetBotConfig().DatabaseLink);
    public static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private Program()
    {
        Client = new DiscordSocketClient( new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.GuildMessages
                             | GatewayIntents.GuildWebhooks
                             | GatewayIntents.DirectMessageTyping
                             | GatewayIntents.Guilds
        });
        _botConfig = BotConfig.GetBotConfig();
            
        Client.Log += Logger.DiscordLog;
        Client.JoinedGuild += GuildJoined;
        Client.LeftGuild += GuildLeft;
        Client.ButtonExecuted += arg => Tools.RunCommandInBackground(arg, () => ButtonExecuted(arg));
        Client.Ready += () => Tools.ExceptionHandler(Ready, true);
        Client.SlashCommandExecuted += arg => Tools.RunCommandInBackground(arg, () => SlashCommands.SlashCommandHandler(arg));
        Client.SelectMenuExecuted += arg => Tools.RunCommandInBackground(arg, () => SelectMenuExecuted(arg));
    }

    public static Program GetInstance()
    {
        return _instance ??= new Program();
    }
        
    private async Task Start()
    {
        await Client.LoginAsync(TokenType.Bot, _botConfig.BotToken);
        await Client.StartAsync();
        await Client.SetGameAsync("/help");
    }

    private async Task Ready()
    {
        if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "init")
        {
            await SlashCommands.InitSlashCommands();
            Logger.Log(new MyLogMessage(LogSeverity.Info, "Ready", "Initialized all commands."));
        }
        
        await Config.ServerConfigStartUp();
        _bgTask ??= BgTask();
    }

    private static async Task ButtonExecuted(SocketMessageComponent arg)
    {
        string matchLink = "";
        foreach (Embed e in arg.Message.Embeds)
        {
            matchLink = ((EmbedAuthor)e.Author!).Url;
        }

        GetMatch request = new GetMatch { Id = Tools.GetIdFromUrl(matchLink) };
        Match match = await request.SendRequest<Match>();

        if (arg.Data.CustomId == "overallstats_bo1")
        {
            GetMatchMapStats requestMapStats = new GetMatchMapStats { Id = match.Maps[0].StatsId };
            await arg.ModifyOriginalResponseAsync(msg =>
                msg.Embed = requestMapStats.SendRequest<MatchMapStats>().Result.ToEmbed());
        }
        else
        {
            GetMatchStats requestMatchStats = new GetMatchStats { Id = match.StatsId };
            await arg.ModifyOriginalResponseAsync(msg =>
                msg.Embed = requestMatchStats.SendRequest<MatchStats>().Result.ToEmbed());
        }
    }
    
    private static async Task SelectMenuExecuted(SocketMessageComponent arg)
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
    }
    
    public Task GuildJoined(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Config.GuildJoined(guild);
                StatsTracker.GetStats().ServerCount = Client.Guilds.Count;
            }
            catch (Exception ex)
            {
                if (ex is HttpException)
                {
                    Logger.Log(new MyLogMessage(LogSeverity.Info, "GuildJoined", "Found server with insufficient permissions."));
                    await Config.SendMessageAfterServerJoin(guild, new EmbedBuilder()
                        .WithDescription(
                            "It looks like the bot has insufficient permissions (probably webhooks) on this server" +
                            ". Please use the invite-link and grant all requested permissions.").Build());
                }
                else
                {
                    Logger.Log(new MyLogMessage(LogSeverity.Error, ex));
                    await Config.SendMessageAfterServerJoin(guild, new EmbedBuilder()
                        .WithDescription(
                            $"An Exception occured while joining this server with the following message: {ex.Message}. Please report this bug!")
                        .Build());
                }
                throw;
            }
        });
        return Task.CompletedTask;
    }

    private Task GuildLeft(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            await Tools.ExceptionHandler(async () =>
            {
                await ServerConfigRepository.Delete(guild.Id);
                StatsTracker.GetStats().ServerCount = Client.Guilds.Count;
            });
        });
        
        return Task.CompletedTask;
    }

    private async Task MiscellaneousBackground()
    {
        if (Client.CurrentUser.Id == _botConfig.ProductionBotId)
        {
            //top.gg
            HttpRequestMessage reqTop = new(HttpMethod.Post, $"https://top.gg/api/bots/${Client.CurrentUser.Id}/stats");
            reqTop.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botConfig.TopGgApiKey);
            reqTop.Content = new StringContent($"{{ \"server_count\": {Client.Guilds.Count} }}", Encoding.UTF8, "application/json");
            await DefaultHttpClient.SendAsync(reqTop);
            
            //bots.gg
            HttpRequestMessage reqBots = new (HttpMethod.Post, $"https://discord.bots.gg/api/v1/bots/${Client.Guilds.Count}/stats");
            reqBots.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_botConfig.BotsGgApiKey);
            reqBots.Content = new StringContent($"{{ \"guildCount\": {Client.Guilds.Count} }}", Encoding.UTF8, "application/json");
            await DefaultHttpClient.SendAsync(reqBots);
        }
        
        CacheCleaner.Clean();
        StatsRepository.Update(StatsTracker.GetStats());
        await HltvRanking.UpdateTeamRanking();
    }

    private async Task BgTask()
    {
        await Tools.ExceptionHandler(MiscellaneousBackground, true);
        
        Timer updateGgStatsTimer = new(1000 * 60 * 60); // 1h interval
        updateGgStatsTimer.Elapsed += async (_, _) =>
        {
            await Tools.ExceptionHandler(MiscellaneousBackground, true);
        };
        updateGgStatsTimer.Enabled = true;
        
        
        (Timer, Func<Task>)[] timers = {(new Timer(), HltvNews.SendNewNews),// (new Timer(), HltvResults.SendNewResults),
            (new Timer(), HltvEvents.SendNewStartedEvents), (new Timer(), HltvEvents.SendNewPastEvents), (new Timer(), HltvMatches.UpdateMatches)};
        foreach ((Timer timer, Func<Task> function) in timers)
        {
            await Tools.ExceptionHandler(function, true);
            timer.Interval = _botConfig.CheckResultsTimeInterval;
            timer.Elapsed += async (_, _) =>
                await Tools.ExceptionHandler(function, true);
            
            timer.Enabled = true;
            await Task.Delay(_botConfig.CheckResultsTimeInterval / timers.Length);
        }
    }
}