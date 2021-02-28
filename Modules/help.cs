using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpCommand (string arg = "")
        {
            Config _cfg = new Config();
            string prefix = "!";
            if (Context.Guild != null) { prefix = _cfg.GetServerConfig(Context.Guild).Prefix; }
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.DarkMagenta)
                .WithCurrentTimestamp();
            switch(arg.ToLower())
            {
                case "prefix":
                    builder.WithTitle("HELP PREFIX")
                        .AddField("Syntax:", $"\"{prefix}prefix [prefix]\"")
                        .AddField("Summary:", $"Sets the prefix\nexample:\n\"{prefix}prefix ~\"")
                        .AddField("Permissions:", "Admin");
                    break;
                case "init":
                    builder.WithTitle("HELP INIT")
                        .AddField("Syntax:", $"\"{prefix}init [Textchannel (leave blank for current channel)]\"")
                        .AddField("Summary:", $"Sets the default channel for HLTV-NEWS\nexample:\n\"{prefix}init {((SocketTextChannel)Context.Channel).Mention}\"")
                        .AddField("Permissions:", "ManageChannels");
                    break;
                case "player":
                    builder.WithTitle("HELP PLAYER")
                        .AddField("Syntax:", $"\"{prefix}player [name]\"")
                        .AddField("Summary:", $"Gets the stats of a player\nfirst time requests may take up to 10 seconds\nexample:\n\"{prefix}player tabsen\"")
                        .AddField("Permissions:", "none");
                    break;
                case "minstars":
                    builder.WithTitle("HELP MINSTARS")
                        .AddField("Syntax:", $"\"{prefix}minstars [stars (number betweeen 0-5)]\"")
                        .AddField("Summary:", $"Changes the minimum stars of a match to be displayed in your HLTV-News-Feed\nexample:\n\"{prefix}minstars 0\"")
                        .AddField("Permissions:", "Admin");
                    break;
                case "ranking":
                    builder.WithTitle("HELP RANKING")
                        .AddField("Syntax:", $"\"{prefix}ranking [number from 1-30 (default = 10)] [country or region (default = GLOBAL)]\"")
                        .AddField("Summary:", $"Displays the team ranking of a specific country or region \nexample:\n\"{prefix}ranking 15 germany\"\n\"{prefix}ranking 3 united states\"")
                        .AddField("Permissions:", "none");
                    break;
                case "upcoming":
                    builder.WithTitle("HELP UPCOMING")
                        .AddField("Syntax:", $"\"{prefix}upcoming [date or team or event or leave blank for all upcoming matches]\"")
                        .AddField("Summary:", $"Displays the upcoming matches for a day in the future, a team or an event \nexample:\n\"{prefix}upcoming 9.2\"\n\"{prefix}upcoming g2\"")
                        .AddField("Permissions:", "none");
                    break;
                case "event":
                    builder.WithTitle("HELP EVENT")
                        .AddField("Syntax:", $"\"{prefix}event [eventname or parts of the eventname]\"")
                        .AddField("Summary:", $"Displays information about a specific event \nexample:\n\"{prefix}event blast\"")
                        .AddField("Permissions:", "none");
                    break;
                case "events":
                    builder.WithTitle("HELP EVENTS")
                        .AddField("Syntax:", $"\"{prefix}events\"")
                        .AddField("Summary:", $"Displays all ongoing events\nexample:\n\"{prefix}events\"")
                        .AddField("Permissions:", "none");
                    break;
                case "upcomingevents":
                    builder.WithTitle("HELP UPCOMINGEVENTS")
                        .AddField("Syntax:", $"\"{prefix}upcomingevents\"")
                        .AddField("Summary:", $"Displays upcoming events for the next 30 days \nexample:\n\"{prefix}upcomingevents\"")
                        .AddField("Permissions:", "none");
                    break;
                case "live":
                    builder.WithTitle("HELP LIVE")
                        .AddField("Syntax:", $"\"{prefix}live\"")
                        .AddField("Summary:", $"Displays all live matches and their livestreams \nexample:\n\"{prefix}live\"")
                        .AddField("Permissions:", "none");
                    break;
                default:
                    builder.WithTitle("HELP")
                        .AddField("Commands:", $"\"{prefix}init\"\n\"{prefix}player\"\n\"{prefix}minstars\"\n\"{prefix}ranking\"\n\"{prefix}about\"\n\"{prefix}upcoming\"\n\"{prefix}event\"\n\"{prefix}events\"\n\"{prefix}upcomingevents\"\n\"{prefix}live\"\n\"{prefix}prefix\"", true)
                        .AddField("Short summary:", $"sets the default channel for HLTV-NEWS\ngives stats for a player\nsets minimumstars for HLTV matches to output\n" +
                        $"displays the team ranking\ndisplays the contact page\ndisplays upcoming matches\ngives information about a specific event\nshows all ongoing events\n" +
                        $"shows upcoming events for the next 30 days\nshows all live matches\nchanges the command prefix", true)
                        .WithFooter($"For more info type: \"{prefix}help [command]\"");
                    break;
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
