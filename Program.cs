using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
        private Hltv _hltv;
        private HltvNews _hltvNews;
        private HltvEvents _hltvevents;
        private HltvLive _hltvlive;
        private Config _cfg;
        private CacheCleaner _cl;
        private Upcoming _upcoming;
        private ConfigClass Botconfig;

        public async Task RunBotAsync()
        {
            DiscordSocketConfig _config = new DiscordSocketConfig() { };
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _hltv = new Hltv();
            _hltvNews = new HltvNews();
            _hltvevents = new HltvEvents();
            _cfg = new Config();
            _cl = new CacheCleaner();
            _upcoming = new Upcoming();
            _hltvlive = new HltvLive();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            Botconfig = _cfg.LoadConfig();

            string BotToken = Botconfig.BotToken;

            _client.Log += Log;
            _client.ReactionAdded += ReactionAdd;
            _client.JoinedGuild += GuildJoined;
            _client.LeftGuild += GuildLeft;

            //catch if serverconfigs exist
            foreach(SocketGuild guild in _client.Guilds)
            {
                await _cfg.GuildJoined(guild, null, true);
            }


            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();

            await _client.SetGameAsync("!help");

            //await _scoreboard.ConnectWebSocket();

            await BGTask();

            await Task.Delay(-1);
        }

        private async Task GuildLeft(SocketGuild arg)
        {
            _cl.Cleaner(_client);
        }

        private async Task GuildJoined(SocketGuild guild)
        {
            await _cfg.GuildJoined(guild);
        }

        private async Task BGTask()
        {
            await Task.Delay(3000);
            bool updateTopGG = true;
            while (true)
            {
                //top.gg API
                if(DateTime.Now.Hour == 0 && updateTopGG && _client.CurrentUser.Id == 807182830752628766)
                {
                    updateTopGG = false;
                    HttpClient http = new HttpClient();
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Botconfig.topGGApiKey);
                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "https://top.gg/api/bots/807182830752628766/stats");
                    req.Content = new StringContent($"{{ \"server_count\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                    await http.SendAsync(req);
                } else if(DateTime.Now.Hour == 1) { updateTopGG = true; }
#if RELEASE
                await _hltv.AktHLTV(await _cfg.GetChannels(_client), _client);
                await _hltvNews.aktHLTVNews(await _cfg.GetChannels(_client));
                await _hltvevents.AktEvents(await _cfg.GetChannels(_client));
                await _hltvevents.GetUpcomingEvents();
                await _upcoming.UpdateUpcomingMatches();
#endif
                _cl.Cleaner(_client);
                Console.WriteLine($"{DateTime.Now.ToString().Substring(11)} HLTV\t\tFeed aktualisiert");
                await Task.Delay(Botconfig.CheckResultsTimeInterval);
            }
        }

        private async Task ReactionAdd(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage msg;
            try { msg = await cacheable.GetOrDownloadAsync(); }
            catch (Discord.Net.HttpException) { return; }
            if (!msg.Author.IsBot || reaction.User.Value.IsBot) { return; }

            string[] numberEmoteStrings = { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣" };
            foreach (string emoteString in numberEmoteStrings)
            {
                if (emoteString == reaction.Emote.ToString()) { _hltvlive.startScoreboard(msg, new Emoji(reaction.Emote.ToString()), (channel as SocketGuildChannel).Guild); return; }
            }

            if (reaction.Emote.Name != "hltvstats") { return; }
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
                await _hltv.stats(embedReac.Author.Value.Url, (ITextChannel)reaction.Channel);
            }
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);

            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage Message = arg as SocketUserMessage;

            if (Message is null || Message.Author.IsBot)
                return;

            int argPos = 0;
            string prefix;
            if (Message.Channel as SocketGuildChannel == null) { prefix = "!"; }
            else { prefix = _cfg.GetServerConfig((Message.Channel as SocketGuildChannel).Guild).Prefix; }

            if (Message.HasStringPrefix(prefix, ref argPos) || Message.HasStringPrefix($"{prefix} ", ref argPos) || Message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                SocketCommandContext context = new SocketCommandContext(_client, Message);
                IResult Result = await _commands.ExecuteAsync(context, argPos, _services);

                //Log Commands
                FileStream fs = File.OpenWrite($"./cache/log/{DateTime.Now.ToShortDateString()}.txt"); fs.Close();
                string ori = File.ReadAllText($"./cache/log/{DateTime.Now.ToShortDateString()}.txt");
                File.WriteAllText($"./cache/log/{DateTime.Now.ToShortDateString()}.txt", ori + DateTime.Now.ToShortTimeString() + " " + Message.Channel.ToString() + " " + Message.ToString() + "\n");

                if (!Result.IsSuccess)
                    Console.WriteLine(Result.ErrorReason);
            }
        }
    }
}
