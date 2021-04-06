using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvRanking : ModuleBase<SocketCommandContext>
    {
        [Command("ranking")]
        public async Task GetRanking([Remainder]string arg = "GLOBAL")
        {
            EmbedBuilder embed = new();
            string endpoint;
            if (arg.Contains('-'))
            {
                arg = "";
                foreach (string str in arg.Split('-')) { arg += $"{str} "; }
            }
            if (arg == "GLOBAL")
            {
                endpoint = "ranking";
            }
            else if (DateTime.TryParse(arg, out DateTime time))
            {
                string[] months = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
                endpoint = $"ranking/{time.Day}/{months[time.Month - 1]}/{time.Year}";
            }
            else
            {
                endpoint = "ranking/" + arg.ToLower();
            }

            //cache
            JArray jArr;
            Directory.CreateDirectory("./cache/ranking");
            if(!File.Exists($"./cache/ranking/ranking_{arg.ToLower().Replace(' ','-')}.json"))
            {
                var req = await Tools.RequestApiJArray(endpoint);
                if(!req.Item2) {
                    embed.WithColor(Color.Red)
                        .WithTitle($"error")
                        .WithDescription("Our API is currently not available! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues). We're sorry for the inconvience");
                    await ReplyAsync(embed: embed.Build());
                    return;
                }
                jArr = req.Item1;
                if (jArr.Count == 0) 
                {
                    embed.WithColor(Color.Red)
                    .WithTitle($"{arg} does not exist")
                    .WithDescription("Please state a valid country!");
                    await ReplyAsync(embed: embed.Build());
                    return;
                } else
                {
                    FileStream fs = File.Create($"./cache/ranking/ranking_{arg.ToLower().Replace(' ', '-')}.json");
                    fs.Close();
                    File.WriteAllText($"./cache/ranking/ranking_{arg.ToLower().Replace(' ', '-')}.json", jArr.ToString());
                }
            }
            else
            {
                jArr = JArray.Parse(File.ReadAllText($"./cache/ranking/ranking_{arg.ToLower().Replace(' ', '-')}.json"));
            }

            int teamsDisplayed = jArr.Count;
            string val = "";
            int maxTeams = 10;
            for (int i = 0; i < jArr.Count; i++)
            {
                JObject jObj = JObject.Parse(jArr[i].ToString());
                short change = short.Parse(jObj.GetValue("change").ToString());
                string development = change switch
                {
                    < 0 => "(⬇️ " + Math.Abs(change) + ")",
                    > 0 => "(⬆️ " + Math.Abs(change) + ")",
                    _ => "(⏺️ 0)",
                };
                if(bool.Parse(jObj.GetValue("isNew").ToString())) { development = "(🆕)"; }
                JObject teamJObj = JObject.Parse(JObject.Parse(jArr[i].ToString()).GetValue("team").ToString());
                string teamLink = $"https://www.hltv.org/team/{teamJObj.GetValue("id")}/{teamJObj.GetValue("name").ToString().Replace(' ', '-')}";
                val += $"{i + 1}.\t[{teamJObj.GetValue("name")}]({teamLink}) {development}\n";
                if(i + 1 == maxTeams)
                {
                    teamsDisplayed = i + 1;
                    break;
                }
            }
            embed.WithTitle($"TOP {teamsDisplayed} {arg.ToUpper()}")
                .AddField("teams:", val)
                .WithColor(Color.Blue)
                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: embed.Build());
        }        
    }
}
