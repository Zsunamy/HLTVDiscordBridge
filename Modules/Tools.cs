using Discord;
using System;
using System.IO;
using Discord.WebSocket;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Tools
    {
        public static EmbedFooterBuilder GetRandomFooter (SocketGuild guild, DiscordSocketClient client)
        {
            EmbedFooterBuilder builder = new();
            string[] footerStrings = File.ReadAllText("./cache/footer.txt").Split("\n");
            Random _rnd = new();
            Config _cfg = new();
            string footerString = footerStrings[_rnd.Next(0, footerStrings.Length)];
            if (footerString.Contains("<prefix>")) { footerString = footerString.Replace("<prefix>", _cfg.GetServerConfig(guild).Prefix); }
            if (footerString.Contains("<servercount>")) { footerString = footerString.Replace("<servercount>", client.Guilds.Count.ToString()); }
            int totalUser = 0;
            foreach (SocketGuild g in client.Guilds)
            {
                totalUser += g.Users.Count;
            }
            if (footerString.Contains("<playercount>")) { footerString = footerString.Replace("<playercount>", totalUser.ToString()); }
            builder.Text = footerString;
            return builder;
        }

        public static async Task<(JObject, bool)> RequestApiJObject(string endpoint)
        {
            HttpClient http = new();
            Uri uri = new($"{Config.LoadConfig().APILink}/api/{endpoint}");
            HttpResponseMessage resp = await http.GetAsync(uri);
            string resultString = await resp.Content.ReadAsStringAsync();
            if(resultString == "404")
            {
                //not found
                return (null, true);
            }
            else if(resultString == "403" || resultString == "error")
            {
                //cloudflare ban
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} returned cloudflare ban");
                return (null, false);
            } else
            {
                //OK
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                return (JObject.Parse(resultString), true);
            }
        }
        public static async Task<(JArray, bool)> RequestApiJArray(string endpoint)
        {
            HttpClient http = new();
            Uri uri = new($"{Config.LoadConfig().APILink}/api/{endpoint}");
            HttpResponseMessage resp = await http.GetAsync(uri);
            string resultString = await resp.Content.ReadAsStringAsync();
            if (resultString == "404")
            {
                //not found
                return (null, true);
            }
            else if (resultString == "403" || resultString == "error")
            {
                //cloudflare ban
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} returned cloudflare ban");
                return (null, false);
            }
            else
            {
                //OK
                Console.WriteLine($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                return (JArray.Parse(resultString), true);
            }
        }
    }
}
