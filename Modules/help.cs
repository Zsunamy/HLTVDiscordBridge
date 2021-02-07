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
                case "":
                    break;
                case "init":
                    builder.WithTitle("HELP INIT")
                        .AddField("Syntax:", "\"!init [Textchannel (leave blank for current channel)]\"")
                        .AddField("Summary:", $"sets the default channel for HLTV-NEWS\nexample: \"!init {((SocketTextChannel)Context.Channel).Mention}\"")
                        .AddField("Permissions:", "ManageChannels");
                    break;
                case "player":
                    builder.WithTitle("HELP PLAYER")
                        .AddField("Syntax:", "\"!player [name]\"")
                        .AddField("Summary:", "gets the stats of a player\nexample: \"!player tabsen\"")
                        .AddField("Permissions:", "none");
                    break;
                case "minstars":
                    builder.WithTitle("HELP MINSTARS")
                        .AddField("Syntax:", "\"!minstars [stars (number betweeen 0-5)]\"")
                        .AddField("Summary:", "changes the minimum stars of a match to be displayed in your HLTV-News-Feed\nexample: \"!minstars 0\"")
                        .AddField("Permissions:", "Admin");
                    break;
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
