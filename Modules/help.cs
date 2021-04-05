using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpCommand(string arg = "")
        {
            Config _cfg = new();
            string prefix;
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel))) { prefix = "!"; }
            else { prefix = _cfg.GetServerConfig(Context.Guild).Prefix; }
            EmbedBuilder builder = new();
            builder.WithColor(Color.DarkMagenta)
                .WithCurrentTimestamp();
            switch (arg.ToLower())
            {
                case "prefix":
                    builder.WithTitle($"advanced help for {prefix}prefix")
                        .AddField("syntax:", $"`{prefix}prefix [prefix]`", true)
                        .AddField("example:", $"`{prefix}prefix ?`", true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("summary:", $"Sets a custom prefix for this server.", true)
                        .AddField("permissions:", "admin", true)
                        .AddField("\u200b", "\u200b", true);
                    break;
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
                        .AddField("options:", "`stars`\n`featuredevents`\n`prefix`\n`newsoutput`\n`resultoutput`\n`eventoutput`", true)
                        .AddField("possible states:", "number between 0-5\ntrue/false\nany string\ntrue/false\ntrue/false\ntrue/false", true)
                        .AddField("examples:", $"`{prefix}set prefix $`\n`{prefix}set newsoutput false`\n`{prefix}set stars 3`", true)
                        .AddField("summary:", $"Changes the options for you personal server.", true)
                        .AddField("permissions:", "admin", true)
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
                        .AddField("syntax:", $"`{prefix}event [eventname]`", true)
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
                default:
                    builder.WithTitle("HELP")
                        .AddField("commands:", $"`{prefix}init\n{prefix}player\n{prefix}minstars\n{prefix}ranking\n{prefix}about\n{prefix}upcoming\n{prefix}event\n{prefix}events\n{prefix}upcomingevents\n{prefix}live\n{prefix}prefix`", true)
                        .AddField("short summary:", $"sets the default channel for HLTV-NEWS\ngives information about a specified player\nsets required amount of stars for the automated messages about HLTV matches\n" +
                        $"displays the team ranking\nabout us\ndisplays upcoming matches\ngives information about a specified event\nshows all ongoing events\n" +
                        $"shows upcoming events for the next 30 days\nshows all live matches\nchanges the command prefix", true)
                        .WithFooter($"For more details type: \"{prefix}help [command]\"");
                    break;
            }
            await ReplyAsync(embed: builder.Build());
        }
    }
}
