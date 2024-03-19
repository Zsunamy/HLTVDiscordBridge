using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules;

public static class Commands
{
    public static async Task SendHelpEmbed(SocketSlashCommand arg)
    {
        EmbedBuilder builder = new();
        builder.WithColor(Color.DarkMagenta)
            .WithCurrentTimestamp();
        switch (arg.Data.Options.Count)
        {
            case 0:
                builder.WithTitle("HELP")
                    .AddField("commands:", $"```/init\n/set\n/about\n/ranking\n/upcoming-matches\n/live\n/player\n/team\n/event\n/events\n/upcoming-events```", true)
                    .AddField("short summary:", $"```sets the default channel for HLTV-NEWS\nchanges the options for your server\nabout us\n" +
                                                $"displays the team ranking\ndisplays upcoming matches\nshows all live matches\n" +
                                                $"gives information about a player\ngives information about a team\ngives information about an event\nshows all ongoing events\n" +
                                                $"shows upcoming events```", true)
                    .WithFooter($"For more details type: \"/help [command]\"");
                break;
            case >0:
                switch(arg.Data.Options.First().Value.ToString()?.ToLower())
                {
                    case "player":
                        builder.WithTitle($"advanced help for /player")
                            .AddField("syntax:", $"`/player [required: name]`", true)
                            .AddField("example:", $"`/player Karrigan`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Sends general information about the specified player. This may take up to 30 seconds depending several factors.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "set":
                        builder.WithTitle($"advanced help for /set")
                            .AddField("options:", "```\nstars\nfeaturedevents\nnews\nresults\nevents```", true)
                            .AddField("possible states:", "```number from 0-5\ntrue/false\ntext-channel\ntext-channel\ntext-channel```", true)
                            .AddField("examples:", $"\u200b\n`\n\n`/set news #general`\n\n`/set stars 3`", true)
                            .AddField("summary:", $"Changes the options for you personal server.", true)
                            .AddField("permission:", "ManageGuild", true)
                            .AddField("\u200b", "\u200b", true)
                            .WithFooter($"for more details type: /set");
                        break;
                    case "ranking":
                        builder.WithTitle($"advanced help for /ranking")
                            .AddField("syntax:", $"`/ranking [optional: country or region]`", true)
                            .AddField("examples:", $"`/ranking`\n`/ranking north america`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays the current world ranking or the specified country/region.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "upcoming-matches":
                        builder.WithTitle($"advanced help for /upcoming-matches")
                            .AddField("syntax:", $"`/upcoming-matches [optional: date, team, event]`", true)
                            .AddField("examples:", $"`/upcoming-matches` or `/upcoming-matches 2.2.2021` or\n /upcoming HAVU` or `/upcoming IEM New York 2020 Europe`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays the upcoming matches for the specified date, team or event.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "event":
                        builder.WithTitle($"advanced help for /event")
                            .AddField("syntax:", $"`/event [required: name]`", true)
                            .AddField("example:", $"`/event IEM New York 2020 Europe`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays information about the specified event.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "events":
                        builder.WithTitle($"advanced help for /events")
                            .AddField("syntax:", $"`/events`", true)
                            .AddField("example:", $"`/events`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays all ongoing events.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "upcoming-events":
                        builder.WithTitle($"advanced help for /upcoming-events")
                            .AddField("syntax:", $"`/upcoming-events`", true)
                            .AddField("example:", $"`/upcoming-events`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays upcoming events for the next 30 days.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "live":
                        builder.WithTitle($"advanced help for /live")
                            .AddField("syntax:", $"`/live`", true)
                            .AddField("example:", $"`/live`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays all live matches and their livestreams.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "team":
                        builder.WithTitle($"advanced help for /team")
                            .AddField("syntax:", $"`/team [name]`", true)
                            .AddField("example:", $"`/team astralis`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays information about the specified team.", true)
                            .AddField("permission:", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    case "support":
                        builder.WithTitle($"advanced help for /support")
                            .AddField("syntax:", $"`/support`", true)
                            .AddField("example:", $"`/support`", true)
                            .AddField("\u200b", "\u200b", true)
                            .AddField("summary:", $"Displays general information about the development team.", true)
                            .AddField("permissions", "@everyone", true)
                            .AddField("\u200b", "\u200b", true);
                        break;
                    default:
                        builder.WithTitle("HELP")
                            .AddField("commands:", $"```/set\n/about\n/ranking\n/upcoming-matches\n/live\n/player\n/team\n/event\n/events\n/upcoming-events```", true)
                            .AddField("short summary:", $"```changes the options for your server\nabout us\n" +
                                                        $"displays the team ranking\ndisplays upcoming matches\nshows all live matches\n" +
                                                        $"gives information about a player\ngives information about a team\ngives information about an event\nshows all ongoing events\n" +
                                                        $"shows upcoming events```", true)
                            .WithFooter($"For more details type: \"/help [command]\"");
                        break;
                }
                break;
                
        }
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = builder.Build());
    }
}