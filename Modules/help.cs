using Discord.Commands;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task help ()
        {
            await ReplyAsync("Hier kommt mal Hilfe hin wenn ich Bock hab");
        }
    }
}
