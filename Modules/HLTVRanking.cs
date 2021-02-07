using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HLTVRanking : ModuleBase<SocketCommandContext>
    {
        [Command("ranking")]
        public async Task getRanking(string num = "10", [Remainder]string arg = "GLOBAL")
        {
            EmbedBuilder embed = new EmbedBuilder();
            int number;
            if(!int.TryParse(num, out number))
            {
                arg = num;
                number = 10;
            }
            Uri uri;
            if (arg == "GLOBAL")
            {
                uri = new Uri("https://hltv-api-steel.vercel.app/api/ranking");
            } else
            {
                uri = new Uri("https://hltv-api-steel.vercel.app/api/ranking/" + arg);
            }
            HttpClient httpClient = new HttpClient();            
            httpClient.BaseAddress = uri;
            HttpResponseMessage response = await httpClient.GetAsync(uri);
            JArray jArr = JArray.Parse(await response.Content.ReadAsStringAsync());

            if (jArr.Count == 0)
            {
                embed.WithColor(Color.Red)
                    .WithTitle($"{arg} DOES NOT EXIST")
                    .WithDescription("Please state a valid country!");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            int teamsDisplayed = jArr.Count;
            string val = "";
            for(int i = 0; i < jArr.Count; i++)
            {
                JObject jObj = JObject.Parse(JObject.Parse(jArr[i].ToString()).GetValue("team").ToString());
                val += $"{i + 1}.\t{jObj.GetValue("name")}\n";
                if(i + 1 == number)
                {
                    teamsDisplayed = i + 1;
                    break;
                }
            }
            embed.WithTitle($"TOP {teamsDisplayed} {arg.ToUpper()}")
                .AddField("teams:", val)
                .WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }        
    }
}
