using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpCommand(string arg = "")
        {
            string prefix;
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel))) { prefix = "!"; }
            else { prefix = Config.GetServerConfig(Context.Guild).Prefix; }
            EmbedBuilder builder = new();
            builder.WithColor(Color.DarkMagenta)
                .WithCurrentTimestamp();
            switch (arg.ToLower())
            {
                case "init":
                    builder.WithTitle($"advanced help for {prefix}init")
                        .AddField("syntax:", $"`{prefix}init [optional: #textchannelid]`", true)
                        .AddField("example:", $"`{prefix}init {((SocketTextChannel)Context.Channel).Mention}`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Sets the default channel for all automated messages.", true)
                        .AddField("permissions:", "ManageChannels", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "player":
                    builder.WithTitle($"advanced help for {prefix}player")
                        .AddField("syntax:", $"`{prefix}player [name]`", true)
                        .AddField("example:", $"`{prefix}player Karrigan`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Sends general information about the specified player. This may take up to 30 seconds depending several factors.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "set":
                    builder.WithTitle($"advanced help for {prefix}set")
                        .AddField("options:", "```\nstars\nfeaturedevents\nprefix\nnews\nresults\nevents```", true)
                        .AddField("possible states:", "```number from 0-5\ntrue/false\nany string\ntrue/false\ntrue/false\ntrue/false```", true)
                        .AddField("examples:", $"\u200b\n`{prefix}set prefix $`\n\n`{prefix}set news false`\n\n`{prefix}set stars 3`", true)
                        .AddField("summary:", $"Changes the options for you personal server.", true)
                        .AddField("permissions:", "admin", true)
                        .AddField("\u200b", "\u200b", true)
                        .WithFooter($"for more details type: {prefix}set");
                    break;
                case "ranking":
                    builder.WithTitle($"advanced help for {prefix}ranking")
                        .AddField("syntax:", $"`{prefix}ranking [country or region]`", true)
                        .AddField("examples:", $"`{prefix}ranking`\n`{prefix}ranking north america`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Displays the current world ranking or the specified country/region.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "upcoming":
                    builder.WithTitle($"advanced help for {prefix} upcoming")
                        .AddField("syntax:", $"`{prefix}upcoming [optional: date, team, event]`", true)
                        .AddField("examples:", $"`{prefix}upcoming` or `{prefix}upcoming 2.2.2021` or\n {prefix}upcoming HAVU` or `{prefix}upcoming IEM New York 2020 Europe`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Displays the upcoming matches for the specified date, team or event.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "event":
                    builder.WithTitle($"advanced help for {prefix}event")
                        .AddField("syntax:", $"`{prefix}event [name]`", true)
                        .AddField("example:", $"`{prefix}event IEM New York 2020 Europe`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Displays information about the specified event.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "events":
                    builder.WithTitle($"advanced help for {prefix}events")
                        .AddField("syntax:", $"`{prefix}events`", true)
                        .AddField("example.", $"`{prefix}events`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Displays all ongoing events.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "upcomingevents":
                    builder.WithTitle($"advanced help for {prefix}upcomingevents")
                        .AddField("syntax:", $"`{prefix}upcomingevents`", true)
                        .AddField("example:", $"`{prefix}upcomingevents`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Displays upcoming events for the next 30 days.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "live":
                    builder.WithTitle($"advanced help for {prefix}live")
                        .AddField("syntax:", $"`{prefix}live`", true)
                        .AddField("example.", $"`{prefix}live`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Displays all live matches and their livestreams.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                case "team":
                    builder.WithTitle($"advanced help for {prefix}team")
                        .AddField("syntax:", $"`{prefix}team [name]`", true)
                        .AddField("example.", $"`{prefix}team astralis`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Displays information about the specified team.", true)
                        .AddField("permissions:", "@everyone", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
                default:
                    builder.WithTitle("HELP")
                        .AddField("commands:", $"```{prefix}init\n{prefix}set\n{prefix}about\n{prefix}ranking\n{prefix}upcoming\n{prefix}live\n{prefix}player\n{prefix}team\n{prefix}event\n{prefix}events\n{prefix}upcomingevents```", true)
                        .AddField("short summary:", $"```sets the default channel for HLTV-NEWS\nchanges the options for your server\nabout us\n" +
                        $"displays the team ranking\ndisplays upcoming matches\nshows all live matches\n" +
                        $"gives information about a player\ngives information about a team\ngives information about an event\nshows all ongoing events\n" +
                        $"shows upcoming events```", true)
                        .WithFooter($"For more details type: \"{prefix}help [command]\"");
                    break;
            }
            StatsUpdater.StatsTracker.MessagesSent += 1;
            StatsUpdater.UpdateStats();
            await ReplyAsync(embed: builder.Build());
        }
    }
}