using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Upcoming : ModuleBase<SocketCommandContext>
    {
        [Command("upcoming")]
        public async Task GetUpcoming([Remainder] string arg = "")
        {
            //Ausgabe nach Team oder Event oder Tag
            await ReplyAsync("", false, BuildEmbed(arg));
        }

        private Embed BuildEmbed(string arg)
        {
            JArray jArr;
            EmbedBuilder builder = new EmbedBuilder();
            DateTime date;
            if (DateTime.TryParse(arg, out date))
            {
                jArr = SearchUpcoming(date);
                builder.WithTitle($"UPCOMING MATCHES FOR {date.Date.ToString().Substring(0, 10)}");
            }
            else if(arg == "") { builder.WithTitle($"UPCOMING MATCHES"); jArr = SearchUpcoming(); }
            else { builder.WithTitle($"UPCOMING MATCHES FOR {arg.ToUpper()}"); jArr = SearchUpcoming(arg); }
            if (jArr.Count == 0)
            {
                builder.WithDescription("there are no upcoming matches");
            } else if(jArr.Count == 1)
            {
                JObject jObj = JObject.Parse(jArr[0].ToString());
                string team1name = JObject.Parse(JArray.Parse(jObj.GetValue("teams").ToString())[0].ToString()).GetValue("name").ToString();
                string team2name = JObject.Parse(JArray.Parse(jObj.GetValue("teams").ToString())[1].ToString()).GetValue("name").ToString();
                string format = jObj.GetValue("map").ToString();
                string eventname = JObject.Parse(JObject.Parse(jObj.GetValue("event").ToString()).ToString()).GetValue("name").ToString();
                string link = jObj.GetValue("link").ToString();
                DateTime time = DateTime.Parse(jObj.GetValue("time").ToString());
                builder.AddField("match:", $"{team1name} vs. {team2name}")
                    .AddField("event:", eventname)
                    .AddField("time:", time.ToString().Substring(0, 16), true)
                    .AddField("format:", format, true)
                    .AddField("link:", $"https://hltv.org{link}");
            } else if(jArr.Count > 1)
            {
                int i = 0;
                foreach (JToken jTok in jArr)
                {
                    if (i == 2) { break; }
                    JObject jObj = JObject.Parse(jTok.ToString());
                    string team1name = JObject.Parse(JArray.Parse(jObj.GetValue("teams").ToString())[0].ToString()).GetValue("name").ToString();
                    string team2name = JObject.Parse(JArray.Parse(jObj.GetValue("teams").ToString())[1].ToString()).GetValue("name").ToString();
                    string format = jObj.GetValue("map").ToString();
                    string eventname = JObject.Parse(JObject.Parse(jObj.GetValue("event").ToString()).ToString()).GetValue("name").ToString();
                    string link = jObj.GetValue("link").ToString();
                    DateTime time = DateTime.Parse(jObj.GetValue("time").ToString());
                    builder.AddField("match:", $"{team1name} vs. {team2name}", true)
                        .AddField("time:", time.ToString().Substring(0, 16), true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("event:", eventname, true)
                        .AddField("format:", format, true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("details:", $"[click here for more details](https://hltv.org{link})");
                    if (i == 0) {  }
                    i++;
                }

                
                builder.WithFooter($"and {jArr.Count - 2} more");
            }
            builder.WithCurrentTimestamp()
                .WithColor(Color.Blue);
            return builder.Build();
        }

        private JArray SearchUpcoming()
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            JArray result = JArray.Parse("[]");
            foreach(JToken jTok in jArr)
            {
                if (DateTime.Parse(JObject.Parse(jTok.ToString()).GetValue("time").ToString()).CompareTo(DateTime.Now) != -1)
                {
                    result.Add(jTok);
                }
            }
            return result;
        }
        private JArray SearchUpcoming(string arg)
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            JArray result = JArray.Parse("[]");
            foreach (JToken jTok in jArr)
            {
                string eventname = JObject.Parse(JObject.Parse(jTok.ToString()).GetValue("event").ToString()).GetValue("name").ToString().ToLower();
                string team1name = JObject.Parse(JArray.Parse(JObject.Parse(jTok.ToString()).GetValue("teams").ToString())[0].ToString()).GetValue("name").ToString().ToLower();
                string team2name = JObject.Parse(JArray.Parse(JObject.Parse(jTok.ToString()).GetValue("teams").ToString())[1].ToString()).GetValue("name").ToString().ToLower();
                if (arg.ToLower() == eventname || arg.ToLower() == team1name || arg.ToLower() == team2name)
                {
                    if (DateTime.Parse(JObject.Parse(jTok.ToString()).GetValue("time").ToString()).CompareTo(DateTime.Now) != -1) { result.Add(jTok); }                    
                }
            }
            return result;
        }
        private JArray SearchUpcoming(DateTime date)
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            JArray result = JArray.Parse("[]");
            foreach (JToken jTok in jArr)
            {
                string time = JObject.Parse(jTok.ToString()).GetValue("time").ToString().Substring(0,10);
                if(time.Substring(0,10) == date.ToString().Substring(0, 10))
                {
                    if (DateTime.Parse(JObject.Parse(jTok.ToString()).GetValue("time").ToString()).CompareTo(DateTime.Now) != -1) { result.Add(jTok); }
                }                
            }
            return result;
        }
    }
}
