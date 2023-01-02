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
            { GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites });
        _botConfig = BotConfig.GetBotConfig();
            
        Client.Log += Log;
        Client.JoinedGuild += GuildJoined;
        Client.LeftGuild += GuildLeft;
        Client.ButtonExecuted += ButtonExecuted;
        Client.Ready += Ready;
        Client.SlashCommandExecuted += SlashCommands.SlashCommandHandler;
        Client.SelectMenuExecuted += SelectMenuExecuted;
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

    private static Task SelectMenuExecuted(SocketMessageComponent arg)
    {
        _ = Task.Run(async () =>
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
        return Task.CompletedTask;
    }

    private async Task Ready()
    {
        if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "init")
        {
            await SlashCommands.InitSlashCommands();
            WriteLog($"{DateTime.Now.ToLongTimeString()} Init\t\t successfully initialized all commands");
        }
        
        await Config.ServerConfigStartUp();
        _bgTask ??= BgTask();
    }

    private static Task ButtonExecuted(SocketMessageComponent arg)
    {
        Task handler = Task.Run(async () =>
        {
            await arg.DeferAsync();
            string matchLink = "";
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

    private Task GuildLeft(SocketGuild guild)
    {
        IMongoCollection<ServerConfig> collection = Config.GetCollection();
        collection.DeleteOne(x => x.GuildID == guild.Id);
        StatsUpdater.StatsTracker.Servercount = Client.Guilds.Count;
        return Task.CompletedTask;
    }

    public Task GuildJoined(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Config.GuildJoined(guild);
                StatsUpdater.StatsTracker.Servercount = Client.Guilds.Count;
            }
            catch (Exception ex)
            {
                if (ex is Discord.Net.HttpException)
                {
                    await Config.SendMessageAfterServerJoin(guild, new EmbedBuilder()
                        .WithDescription(
                            "It looks like the bot has insufficient permissions (probably webhooks) on this" +
                            "server. Please use the invite-link and grant all requested permissions.").Build());
                    throw;
                }
                Console.WriteLine(ex);
                await Config.SendMessageAfterServerJoin(guild, new EmbedBuilder()
                    .WithDescription(
                        $"An {ex.Message} Exception occured while joining this server. Please report this bug ")
                    .Build());
                throw;
            }
            
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
        StatsUpdater.UpdateStats();
        await HltvRanking.UpdateTeamRanking();
    }

    private async Task BgTask()
    {
        try
        {
            await MiscellaneousBackground();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        Timer updateGgStatsTimer = new(1000 * 60 * 60);
        updateGgStatsTimer.Elapsed += async (sender, e) =>
        {
            try
            {
                await MiscellaneousBackground();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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