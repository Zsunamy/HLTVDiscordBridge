using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public static class SlashCommands
    {
        private static DiscordSocketClient _client = null;
        public static async Task InitSlashCommands(DiscordSocketClient client)
        {
            _client = client;

            ulong guildId = 792139588743331841;

            var guildSupportCommand = new SlashCommandBuilder()
                .WithName("support")
                .WithDescription("Sends the support page.");
            await client.Rest.CreateGuildCommand(guildSupportCommand.Build(), guildId);

            var guildEventCommand = new SlashCommandBuilder()
            .WithName("event")
            .WithDescription("Gives you selected information about an event.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("name")
                .WithDescription("name of event")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
            );
            await client.Rest.CreateGuildCommand(guildEventCommand.Build(), guildId);

            var guildTeamCommand = new SlashCommandBuilder()
                .WithName("team")
                .WithDescription("Gives you selected information about a team.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("name")
                    .WithDescription("name of team")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
                    );
            await client.Rest.CreateGuildCommand(guildTeamCommand.Build(), guildId);

            var guildPlayerCommand = new SlashCommandBuilder()
            .WithName("player")
            .WithDescription("Gives you selected information about a player.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("name")
                .WithDescription("name of player")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
            );
            await client.Rest.CreateGuildCommand(guildPlayerCommand.Build(), guildId);

            var guildHelpCommand = new SlashCommandBuilder()
                .WithName("help")
                .WithDescription("Gives you help about our commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("commandname")
                    .WithDescription("name of the command")
                    .WithRequired(false)
                    .AddChoice("general", "general")
                    .AddChoice("init", "init")
                    .AddChoice("player", "player")
                    .AddChoice("set", "set")
                    .AddChoice("ranking", "ranking")
                    .AddChoice("upcoming", "upcoming")
                    .AddChoice("event", "event")
                    .AddChoice("events", "events")
                    .AddChoice("upcomingevents", "upcomingevents")
                    .AddChoice("live", "live")
                    .AddChoice("team", "team")
                    .WithType(ApplicationCommandOptionType.String)
                );
            await client.Rest.CreateGuildCommand(guildHelpCommand.Build(), guildId);

            var guildInitCommand = new SlashCommandBuilder()
                .WithName("init")
                .WithDescription("Initializes the bot to the given Channel")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("channel")
                    .WithDescription("the channel the bot will send the messages")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Channel)
                );
            await client.Rest.CreateGuildCommand(guildInitCommand.Build(), guildId);

            var guildSetCommand = new SlashCommandBuilder()
                .WithName("set")
                .WithDescription("sets options within the bot")
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
                    );

            await client.Rest.CreateGuildCommand(guildSetCommand.Build(), guildId);

            var guildEventsCommand = new SlashCommandBuilder()
                .WithName("events")
                .WithDescription("Dropdown of all ongoing events");

            await client.Rest.CreateGuildCommand(guildEventsCommand.Build(), guildId);

            var guildUpcomingEventsCommand = new SlashCommandBuilder()
                .WithName("upcomingevents")
                .WithDescription("Dropdown of all upcoming events");

            await client.Rest.CreateGuildCommand(guildUpcomingEventsCommand.Build(), guildId);

            var guildUpdateCommand = new SlashCommandBuilder()
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
                    );
            var updateCommand = await client.Rest.CreateGuildCommand(guildUpdateCommand.Build(), guildId);
            ApplicationCommandPermission permission = new(await client.GetUserAsync(248110264610848778), true);
            ApplicationCommandPermission[] permissions = new ApplicationCommandPermission[] { permission };
            //await updateCommand.ModifyCommandPermissions(permissions);

            var guildUpcomingMatchesCommand = new SlashCommandBuilder()
                .WithName("upcomingmatches")
                .WithDescription("gets upcoming matches")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("teamdateevent")
                    .WithDescription("team/date/event")
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.String)
                    );
            await client.Rest.CreateGuildCommand(guildUpcomingMatchesCommand.Build(), guildId);

            var guildRankingCommand = new SlashCommandBuilder()
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
                    );
            await client.Rest.CreateGuildCommand(guildRankingCommand.Build(), guildId);
        }
        public async static Task SlashCommandHandler(SocketSlashCommand arg)
        {
            if (_client == null) { throw new InvalidOperationException("client wasn't initialized"); }
            
            Task.Run(async () =>
            {
                switch (arg.CommandName)
                {
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
                        await SupportCommand.DispSupport(arg);
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
                        await HltvUpcomingMatches.SendUpcomingMatches(arg);
                        break;
                    case "ranking":
                        await HltvRanking.SendRanking(arg);
                        break;

                }
            });
        }
    }
}
