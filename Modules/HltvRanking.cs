using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvRanking : ModuleBase<SocketCommandContext>
    {
        private static readonly string[] Months = { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
        
        public static async Task SendRanking(SocketSlashCommand cmd)
        {
            await cmd.DeferAsync();
            EmbedBuilder embedBuilder = new();
            List<string> properties = new();
            List<string> values = new();
            DateTime now = DateTime.UtcNow;
            DateTime lastMonday = GetLastMonday(now.Day.ToString(), now.Month.ToString(), now.Year.ToString());
            string region = "GLOBAL";

            foreach(SocketSlashCommandDataOption opt in cmd.Data.Options)
            { 
                if(opt.Name == "date" && DateTime.TryParse(opt.Value.ToString(), out DateTime time))
                {
                    lastMonday = GetLastMonday(time.Day.ToString(), time.Month.ToString(), time.Year.ToString());
                    properties.Add("year");
                    properties.Add("month");
                    properties.Add("day");
                    values.Add(lastMonday.Year.ToString());
                    values.Add(Months[lastMonday.Month - 1]);
                    var day = lastMonday.Day.ToString().Length == 1 ? $"0{lastMonday.Day}" : lastMonday.Day.ToString();
                    values.Add(day);
                }
                else if(opt.Name == "region")
                {
                    region = opt.Value.ToString();
                    if (region.Contains('-'))
                    {
                        region = "";
                        foreach (string str in region.Split('-')) { region += $"{str} "; }
                    }
                    properties.Add("country");
                    values.Add(region.ToLower());
                }
            }

            JArray jArr;
            try
            {
                jArr = await Tools.RequestApiJArray("getTeamRanking", properties, values);
            } 
            catch(HltvApiException ex)
            {
                await cmd.Channel.SendMessageAsync(embed: ErrorHandling.GetErrorEmbed(ex));
                return;
            }
            
            string val = "";
            int maxTeams = 10;
            for (int i = 0; i < maxTeams && i < jArr.Count; i++)
            {
                JObject jObj = JObject.Parse(jArr[i].ToString());
                string development = "";
                if (bool.Parse(jObj.GetValue("isNew").ToString())) { development = "(🆕)"; }
                else
                {
                    short change = jObj.TryGetValue("change", out JToken changeTok) ? short.Parse(changeTok.ToString()) : (short)-20;
                    development = change switch
                    {
                        < 0 => "(⬇️ " + Math.Abs(change) + ")",
                        > 0 => "(⬆️ " + Math.Abs(change) + ")",
                        _ => "(⏺️ 0)",
                    };
                }
                JObject teamJObj = JObject.Parse(JObject.Parse(jArr[i].ToString()).GetValue("team").ToString());
                string teamLink = $"https://www.hltv.org/team/{teamJObj.GetValue("id")}/{teamJObj.GetValue("name").ToString().Replace(' ', '-')}";
                val += $"{i + 1}.\t[{teamJObj.GetValue("name")}]({teamLink}) {development}\n";
            }
            embedBuilder.WithTitle($"TOP {Math.Max(maxTeams, jArr.Count)} {lastMonday.ToShortDateString()}")
                .AddField("teams:", val)
                .WithColor(Color.Blue)
                .WithFooter(Tools.GetRandomFooter());
            /*StatsUpdater.StatsTracker.MessagesSent += 1;
            StatsUpdater.UpdateStats();*/
            await cmd.DeleteOriginalResponseAsync();
            await cmd.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }        

        private static DateTime GetLastMonday(string day, string month, string year)
        {
            DateTime date = DateTime.Parse($"{day}/{month}/{year}");
            
            while (date.DayOfWeek != DayOfWeek.Monday)
            {
                date = date.AddDays(-1);
            }

            return date;
        }
    }
}
