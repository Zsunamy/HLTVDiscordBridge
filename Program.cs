using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HLTVDiscordBridge.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
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
        private Config _cfg;
        private CacheCleaner _cl;

        public async Task RunBotAsync()
        {
            DiscordSocketConfig _config = new DiscordSocketConfig() { };
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _hltv = new Hltv();
            _hltvNews = new HltvNews();
            _cfg = new Config();
            _cl = new CacheCleaner();

            _cfg.LoadConfig();
            //_cfg.CreateXML();

            await _hltv.UpdateUpcomingMatches();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            string BotToken = _cfg.LoadConfig().BotToken;

            _client.Log += Log;
            _client.UserJoined += AnnounceUserJoined;
            _client.ReactionAdded += ReactionAdd;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();

            await _client.SetGameAsync("!help");

            await BGTask(_client);

            await Task.Delay(-1);
        }

        private async Task BGTask(DiscordSocketClient client)
        {
            await Task.Delay(3000);
            ITextChannel can = (ITextChannel)client.GetChannel(793120730933755965);
            ITextChannel channel = (ITextChannel)client.GetChannel(792139588743331844);

            while (true)
            {
                await _hltv.aktHLTV(can);                    
                await _hltvNews.aktHLTVNews(can);
                //await _hltv.aktHLTV(channel);
                //await _hltvNews.aktHLTVNews(channel);
                _cl.Cleaner();
                Console.WriteLine($"{DateTime.Now.ToString().Substring(11)} HLTV\t\tFeed aktualisiert");
                await Task.Delay(_cfg.LoadConfig().CheckResultsTimeInterval);
            }
        }

        private async Task ReactionAdd(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage msg = await cacheable.GetOrDownloadAsync();
            string _em = "";
            IEmbed embedReac = null;
            foreach (IEmbed em in msg.Embeds)
            {
                _em = em.ToString();
                embedReac = em;
            }

            if (msg.Author.IsBot && !reaction.User.Value.IsBot && embedReac.Author.Value.Name.ToString().ToLower() == "full details by hltv.org" && reaction.Emote.Name == "stats")
            {
                await _hltv.stats(embedReac.Author.Value.Url, (ITextChannel)reaction.Channel);
            }
        }

        private async Task AnnounceUserJoined(SocketGuildUser user)
        {
            var guild = user.Guild;
            var channel = guild.DefaultChannel;
            await channel.SendMessageAsync($"Welcome {user.Mention}!");
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
            var Message = arg as SocketUserMessage;

            if (Message is null || Message.Author.IsBot)
                return;

            int argPos = 0;

            if (Message.HasStringPrefix("!", ref argPos) || Message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(_client, Message);
                var Result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!Result.IsSuccess)
                    Console.WriteLine(Result.ErrorReason);
            }
        }
    }
}
