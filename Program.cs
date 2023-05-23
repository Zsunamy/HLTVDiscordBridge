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
            //GatewayIntents = (GatewayIntents)536930304
            GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.GuildWebhooks | GatewayIntents.DirectMessageTyping
        });
        _botConfig = BotConfig.GetBotConfig();
            
        Client.Log += Log;
        Client.JoinedGuild += GuildJoined;
        Client.LeftGuild += GuildLeft;
        Client.ButtonExecuted += arg => Tools.RunCommandInBackground(arg, () => ButtonExecuted(arg));
        Client.Ready += Ready;
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
            await Log(new LogMessage(LogSeverity.Info, nameof(Program), "successfully initialized all commands"));
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
                    await Log(new LogMessage(LogSeverity.Warning, "GuildJoined", ex.Message, ex));
                    await Config.SendMessageAfterServerJoin(guild, new EmbedBuilder()
                        .WithDescription(
                            "It looks like the bot has insufficient permissions (probably webhooks) on this" +
                            "server. Please use the invite-link and grant all requested permissions.").Build());
                }
                else
                {
                    await Log(new LogMessage(LogSeverity.Error, "GuildJoined", ex.Message, ex));
                    await Config.SendMessageAfterServerJoin(guild, new EmbedBuilder()
                        .WithDescription(
                            $"An {ex.Message} Exception occured while joining this server. Please report this bug!")
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
                await Config.GetCollection().DeleteOneAsync(x => x.GuildId == guild.Id);
                StatsTracker.GetStats().ServerCount = Client.Guilds.Count;
            }, new LogMessage(LogSeverity.Critical, "GuildLeft", ""));
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
        StatsTracker.GetStats().Update();
        await HltvRanking.UpdateTeamRanking();
    }

    private async Task BgTask()
    {
        await Tools.ExceptionHandler(MiscellaneousBackground,
            new LogMessage(LogSeverity.Warning, "Background-Task", ""));
        
        Timer updateGgStatsTimer = new(1000 * 60 * 60);
        updateGgStatsTimer.Elapsed += async (_, _) =>
        {
            await Tools.ExceptionHandler(MiscellaneousBackground,
                new LogMessage(LogSeverity.Warning, "Background-Task", ""));
        };
        updateGgStatsTimer.Enabled = true;
        
        (Timer, Func<Task>)[] timers = {(new Timer(), HltvNews.SendNewNews), (new Timer(), HltvResults.SendNewResults),
            (new Timer(), HltvEvents.SendNewStartedEvents), (new Timer(), HltvEvents.SendNewPastEvents), (new Timer(), HltvMatches.UpdateMatches)};
        foreach ((Timer timer, Func<Task> function) in timers)
        {
            await Tools.ExceptionHandler(function, new LogMessage(LogSeverity.Critical, "Background-Task", ""));
            timer.Interval = _botConfig.CheckResultsTimeInterval;
            timer.Elapsed += async (_, _) =>
            {
                await Tools.ExceptionHandler(function, new LogMessage(LogSeverity.Critical, "Background-Task", ""));
            };
            timer.Enabled = true;
            await Task.Delay(_botConfig.CheckResultsTimeInterval / timers.Length);
        }
    }
    
    public static async Task Log(LogMessage message)
    {
        if (message.Severity == LogSeverity.Critical)
            await Developer.NotifyCriticalError(message);
        switch (message.Exception)
        {
            case HttpException httpException:
                Console.WriteLine($"[Discord/{message.Severity}] {httpException.Message}"
                                  + $" {httpException.Reason}.");
                break;
            case { } ex:
                Console.WriteLine($"[General/{message.Severity}] {ex.Message} {ex.Source}");
                break;
            default:
                Console.WriteLine($"[General/{message.Severity}] {message}");
                break;
        }
    }

}