using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task help (string arg = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.DarkMagenta)
                .WithCurrentTimestamp();
            switch(arg.ToLower())
            {
                case "init":
                    builder.WithTitle("HELP INIT")
                        .AddField("Syntax:", "\"!init [Textchannel (leave blank for current channel)]\"")
                        .AddField("Summary:", $"Sets the default channel for HLTV-NEWS\nexample:\n\"!init {((SocketTextChannel)Context.Channel).Mention}\"")
                        .AddField("Permissions:", "ManageChannels");
                    break;
                case "player":
                    builder.WithTitle("HELP PLAYER")
                        .AddField("Syntax:", "\"!player [name]\"")
                        .AddField("Summary:", "Gets the stats of a player\nfirst time requests may take up to 10 seconds\nexample:\n\"!player tabsen\"")
                        .AddField("Permissions:", "none");
                    break;
                case "minstars":
                    builder.WithTitle("HELP MINSTARS")
                        .AddField("Syntax:", "\"!minstars [stars (number betweeen 0-5)]\"")
                        .AddField("Summary:", "Changes the minimum stars of a match to be displayed in your HLTV-News-Feed\nexample:\n\"!minstars 0\"")
                        .AddField("Permissions:", "Admin");
                    break;
                case "ranking":
                    builder.WithTitle("HELP RANKING")
                        .AddField("Syntax:", "\"!ranking [number from 1-30 (default = 10)] [country or region (default = GLOBAL)]\"")
                        .AddField("Summary:", "Sisplays the team ranking of a specific country or region \nexample:\n\"!ranking 15 germany\"\n\"!ranking 3 united states\"")
                        .AddField("Permissions:", "none");
                    break;
                default:
                    builder.WithTitle("HELP")
                        .AddField("Commands:", "\"!init\"\n\"!player\"\n\"!minstars\"\n\"!ranking\"\n\"!about\"", true)
                        .AddField("Short summary:", $"sets the default channel for HLTV-NEWS\ngives stats for a player\nsets minimumstars for HLTV matches to output\n" +
                        $"displays the team ranking\nsiplays the contact page", true)
                        .WithFooter("For more info type: \"!help [command]\"");
                    break;
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
