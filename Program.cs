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
        private Config _cfg;
        private ConfigClass Botconfig;

        public async Task RunBotAsync()
        {
            DiscordSocketConfig _config = new DiscordSocketConfig() { };
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _cfg = new Config();

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

        private Task GuildLeft(SocketGuild arg)
        {
            CacheCleaner.Cleaner(_client);
            return Task.CompletedTask;
        }

        private async Task GuildJoined(SocketGuild guild)
        {
            await _cfg.GuildJoined(guild);
        }

        private async Task BGTask()
        {
            await Task.Delay(3000);
            bool updateServerCountGG = true;
            while (true)
            {
                //top.gg API & bots.gg API             
                if(DateTime.Now.Hour == 20 && updateServerCountGG && _client.CurrentUser.Id == 807182830752628766) 
                {
                    updateServerCountGG = false;
                    HttpClient http = new HttpClient();
                    //top.gg
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Botconfig.TopGGApiKey);
                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "https://top.gg/api/bots/807182830752628766/stats");
                    req.Content = new StringContent($"{{ \"server_count\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                    await http.SendAsync(req);
                    //bots.gg
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Botconfig.BotsGGApiKey);
                    req = new HttpRequestMessage(HttpMethod.Post, "https://discord.bots.gg/api/v1/bots/807182830752628766/stats");
                    req.Content = new StringContent($"{{ \"guildCount\": {_client.Guilds.Count} }}", Encoding.UTF8, "application/json");
                    await http.SendAsync(req);
                } else if(DateTime.Now.Hour == 21) { updateServerCountGG = true; }


#if RELEASE
                //await HltvEvents.AktEvents(await _cfg.GetChannels(_client));
                await Upcoming.UpdateUpcomingMatches();
                await HltvResults.AktResults(_client);
                await HltvNews.AktHLTVNews(await _cfg.GetChannels(_client));                           
#endif
                CacheCleaner.Cleaner(_client);
                Console.WriteLine($"{DateTime.Now.ToString().Substring(11)} HLTV\t\tFeed aktualisiert");
                await Task.Delay(Botconfig.CheckResultsTimeInterval);
            }
        }

        private async Task ReactionAdd(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            string[] numberEmoteStrings = { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣" };
            if (!(reaction.Emote.Name == "hltvstats" || numberEmoteStrings.ToString().Contains(reaction.Emote.ToString()))) { return; }

            IUserMessage msg;            
            try { msg = await cacheable.GetOrDownloadAsync(); }
            catch (Discord.Net.HttpException) { return; }
            if (!msg.Author.IsBot || reaction.User.Value.IsBot) { return; }
            
            foreach (string emoteString in numberEmoteStrings)
            {
                if (emoteString == reaction.Emote.ToString()) { HltvLive.StartScoreboard(msg, new Emoji(reaction.Emote.ToString()), (channel as SocketGuildChannel).Guild); return; }
            }
                      
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
                await Hltv.Stats(embedReac.Author.Value.Url, (ITextChannel)reaction.Channel);                
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
            });
        }
    }
}
