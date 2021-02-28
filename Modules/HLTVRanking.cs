﻿using Discord;
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
        public async Task getRanking(string arg = "GLOBAL")
        {
            EmbedBuilder embed = new EmbedBuilder();
            Uri uri;
            DateTime time;
            if (arg.Contains('-'))
            {
                arg = "";
                foreach (string str in arg.Split('-')) { arg += $"{str} "; }
            }
            if (arg == "GLOBAL")
            {
                uri = new Uri("https://hltv-api-steel.vercel.app/api/ranking");
            } else if (DateTime.TryParse(arg, out time))
            {
                string[] months = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
                uri = new Uri($"https://hltv-api-steel.vercel.app/api/ranking/{time.Day}/{months[time.Month - 1]}/{time.Year}");
            } else
            {
                uri = new Uri("https://hltv-api-steel.vercel.app/api/ranking/" + arg);
            }

            //cache
            JArray jArr;
            Directory.CreateDirectory("./cache/ranking");
            if(!File.Exists($"./cache/ranking/ranking_{arg.ToLower()}.json"))
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = uri;
                HttpResponseMessage response = await httpClient.GetAsync(uri);
                try { jArr = JArray.Parse(await response.Content.ReadAsStringAsync()); }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down");
                    embed.WithColor(Color.Red)
                        .WithTitle($"SYSTEM ERROR")
                        .WithDescription("Our API is down! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues).");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                if (jArr.Count == 0)
                {
                    embed.WithColor(Color.Red)
                    .WithTitle($"{arg} DOES NOT EXIST")
                    .WithDescription("Please state a valid country!");
                    await ReplyAsync("", false, embed.Build());
                    return;
                } else
                {
                    FileStream fs = File.Create($"./cache/ranking/ranking_{arg.ToLower()}.json");
                    fs.Close();
                    File.WriteAllText($"./cache/ranking/ranking_{arg.ToLower()}.json", jArr.ToString());
                }
            }
            else
            {
                jArr = JArray.Parse(File.ReadAllText($"./cache/ranking/ranking_{arg.ToLower()}.json"));
            }

            int teamsDisplayed = jArr.Count;
            string val = "";
            int maxTeams = 10;
            for (int i = 0; i < jArr.Count; i++)
            {
                JObject jObj = JObject.Parse(JObject.Parse(jArr[i].ToString()).GetValue("team").ToString());
                string teamLink = $"https://www.hltv.org/team/{jObj.GetValue("id")}/{jObj.GetValue("name").ToString().Replace(' ', '-')}";
                val += $"{i + 1}.\t[{jObj.GetValue("name")}]({teamLink})\n";
                if(i + 1 == maxTeams)
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
