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
        public async Task getRanking()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://hltv-api.revilum.com/ranking");
            HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://hltv-api.revilum.com/ranking"));
            JArray jArr = JArray.Parse(await response.Content.ReadAsStringAsync());

            EmbedBuilder embed = new EmbedBuilder();
            string val = "";
            foreach(JToken jTok in jArr)
            {
                JObject jObj = JObject.Parse(JObject.Parse(jTok.ToString()).GetValue("team").ToString());
                val += $"{jArr.IndexOf(jTok) + 1}.\t{jObj.GetValue("name")}\n";
            }
            embed.WithTitle("TOP 30 GLOBAL")
                .AddField("teams:", val);
            await ReplyAsync("", false, embed.Build());
        }        
    }
}
