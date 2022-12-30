using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class SlashCommands
    {
        private readonly DiscordSocketClient _client;
        public SlashCommands (DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task InitSlashCommands()
        {
            const ulong guildId = 792139588743331841;

            ApplicationCommandProperties[] internalCommands =
            {
                new SlashCommandBuilder()
                    .WithName("servercount")
                    .WithDescription("sends the servercount").Build(),

                new SlashCommandBuilder()
                    .WithName("update")
                    .WithDescription("sends an update to every server")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("version")
                        .WithDescription("version of the bot")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String)
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("text")
                        .WithDescription("text you want to send (Markdown)")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String)
                    ).Build()
            };

            ApplicationCommandProperties[] commands =
            {
                new SlashCommandBuilder()
                    .WithName("support")
                    .WithDescription("Sends the support page.").Build(),

                new SlashCommandBuilder()
                    .WithName("event")
                    .WithDescription("Gives you selected information about an event.")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("name")
                        .WithDescription("name of event")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String)
                    ).Build(),

                new SlashCommandBuilder()
                    .WithName("team")
                    .WithDescription("Gives you selected information about a team.")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("name")
                        .WithDescription("name of team")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String)
                    ).Build(),

                new SlashCommandBuilder()
                    .WithName("player")
                    .WithDescription("Gives you selected information about a player.")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("name")
                        .WithDescription("name of player")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String)
                    ).Build(),

                new SlashCommandBuilder()
                    .WithName("help")
                    .WithDescription("Gives you help about our commands")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("command")
                        .WithDescription("name of the command")
                        .WithRequired(false)
                        .AddChoice("general", "general")
                        .AddChoice("init", "init")
                        .AddChoice("player", "player")
                        .AddChoice("set", "set")
                        .AddChoice("ranking", "ranking")
                        .AddChoice("upcomingmatches", "upcomingmatches")
                        .AddChoice("event", "event")
                        .AddChoice("events", "events")
                        .AddChoice("upcomingevents", "upcomingevents")
                        .AddChoice("live", "live")
                        .AddChoice("team", "team")
                        .AddChoice("support", "support")
                        .WithType(ApplicationCommandOptionType.String)
                    ).Build(),

                new SlashCommandBuilder()
                    .WithName("init")
                    .WithDescription("Initializes the bot to the given Channel")
                    .WithDefaultMemberPermissions(GuildPermission.ManageGuild)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("channel")
                        .WithDescription("the channel the bot will send the messages")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.Channel)
                    ).Build(),

                new SlashCommandBuilder()
                    .WithName("set")
                    .WithDescription("sets options within the bot")
                    .WithDefaultMemberPermissions(GuildPermission.ManageGuild)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("stars")
                        .WithDescription("sets the minimum stars")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("stars")
                            .WithDescription("amount of stars")
                            .WithRequired(true)
                            .WithType(ApplicationCommandOptionType.Integer)
                            .WithMinValue(0)
                            .WithMaxValue(5)
                        )
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("results")
                        .WithDescription("toggles the result messages")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("enabled")
                            .WithDescription("enables/disables result messages")
                            .WithRequired(true)
                            .WithType(ApplicationCommandOptionType.Boolean)
                        )
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("events")
                        .WithDescription("toggles the event messages")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("enabled")
                            .WithDescription("enables/disables event messages")
                            .WithRequired(true)
                            .WithType(ApplicationCommandOptionType.Boolean)
                        )
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("news")
                        .WithDescription("toggles the news messages")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("enabled")
                            .WithDescription("enables/disables news messages")
                            .WithRequired(true)
                            .WithType(ApplicationCommandOptionType.Boolean)
                        )
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("featuredeventsonly")
                        .WithDescription("restrict event messages to only featured events")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("enabled")
                            .WithDescription("enables/disables restriction to only featured events")
                            .WithRequired(true)
                            .WithType(ApplicationCommandOptionType.Boolean)
                        )
                    ).Build(),

                new SlashCommandBuilder()
                    .WithName("events")
                    .WithDescription("Dropdown of all ongoing events").Build(),

                new SlashCommandBuilder()
                    .WithName("upcomingevents")
                    .WithDescription("Dropdown of all upcoming events").Build(),

                new SlashCommandBuilder()
                    .WithName("upcomingmatches")
                    .WithDescription("gets upcoming matches")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("team")
                        .WithDescription("filters for the provided team.")
                        .WithRequired(false)
                        .WithType(ApplicationCommandOptionType.String)
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("event")
                        .WithDescription("filters for the provided event.")
                        .WithRequired(false)
                        .WithType(ApplicationCommandOptionType.String)).Build(),
                
                new SlashCommandBuilder()
                    .WithName("ranking")
                    .WithDescription("gets the ranking of a specified region/date")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("region")
                        .WithDescription("region")
                        .WithRequired(false)
                        .WithType(ApplicationCommandOptionType.String)
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("date")
                        .WithDescription("date")
                        .WithRequired(false)
                        .WithType(ApplicationCommandOptionType.String)
                    ).Build(),
                
                new SlashCommandBuilder()
                    .WithName("live")
                    .WithDescription("shows all live matches").Build(),
            };

            await _client.GetGuild(guildId).BulkOverwriteApplicationCommandAsync(internalCommands);
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(commands);
        }
        public Task SlashCommandHandler(SocketSlashCommand arg)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    switch (arg.CommandName)
                    { 
                        case "servercount":
                            await arg.RespondAsync(_client.Guilds.Count.ToString());
                            break;
                        case "live":
                            await HltvMatches.SendLiveMatches(arg);
                            break;
                        case "player":
                            await HltvPlayer.SendPlayerCard(arg);
                            break;
                        case "event":
                            await HltvEvents.SendEvent(arg);
                            break;
                        case "team":
                            await HltvTeams.SendTeamCard(arg);
                            break;
                        case "support":
                            await SupportCommand.Support(arg);
                            break;
                        case "help":
                            await Commands.SendHelpEmbed(arg);
                            break;
                        case "init":
                            await Config.InitTextChannel(arg);
                            break;
                        case "set":
                            await Config.ChangeServerConfig(arg);
                            break;
                        case "events":
                            await HltvEvents.SendEvents(arg);
                            break;
                        case "upcomingevents":
                            await HltvEvents.SendUpcomingEvents(arg);
                            break;
                        case "update":
                            await Developer.Update(arg, _client);
                            break;
                        case "upcomingmatches":
                            await HltvMatches.SendUpcomingMatches(arg);
                            break;
                        case "ranking":
                            await HltvRanking.SendRanking(arg);
                            break;
                    }
                }
                catch (Exception)
                {
                    await arg.RespondAsync("An error occured!");
                    throw;
                }
                
            });
            return Task.CompletedTask;
        }
    }
}
