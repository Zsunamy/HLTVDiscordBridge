﻿using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Upcoming : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Gets upcoming HLTV matches and their star rating and saves them in ./cache/upcoming.json
        /// </summary>
        /// <returns>All upcoming matches</returns>
        public async Task UpdateUpcomingMatches()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/matches");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpResult = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr;
            try { jArr = JArray.Parse(httpResult); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return; }
            JArray myJArr = new JArray();
            Directory.CreateDirectory("./cache");
            if (!File.Exists("./cache/upcoming.json"))
            {
                FileStream fs = File.Create("./cache/upcoming.json");
                fs.Close();
                File.WriteAllText("./cache/upcoming.json", jArr.ToString());
                return;
            }
            else
            {
                myJArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
                //Console.WriteLine(myJArr);
            }

            foreach (JToken jToken in jArr)
            {
                if (!myJArr.ToString().Contains(JObject.Parse(jToken.ToString()).GetValue("id").ToString()))
                {
                    myJArr.Add(jToken);
                    /*if(myJArr.Contains(JObject.Parse(jToken.ToString()).GetValue("id").ToString()))
                    {
                        myJArr.IndexOf()
                    }*/
                }
            }
            File.WriteAllText("./cache/upcoming.json", myJArr.ToString());
        }
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
                string team1name = JObject.Parse(JObject.Parse(jObj.GetValue("team1").ToString()).ToString()).GetValue("name").ToString();
                if (team1name == "") { team1name = "TBD"; }
                string team2name = JObject.Parse(JObject.Parse(jObj.GetValue("team2").ToString()).ToString()).GetValue("name").ToString();
                if (team2name == "") { team2name = "TBD"; }
                string format = jObj.GetValue("format").ToString();
                string eventname = JObject.Parse(JObject.Parse(jObj.GetValue("event").ToString()).ToString()).GetValue("name").ToString();
                if (eventname == "") { eventname = "n.A"; }
                string link;
                if (team1name != "" && team2name != "" && eventname != "")
                {
                    link = $"https://www.hltv.org/matches/{jObj.GetValue("id")}/{team1name.Replace(' ', '-')}-vs-" +
                    $"{team2name.Replace(' ', '-')}-{eventname.Replace(' ', '-')}";
                } else
                {
                    link = "n.A";
                }
                double time = double.Parse(JObject.Parse(jObj.ToString()).GetValue("date").ToString());
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddMilliseconds(time);
                builder.AddField("match:", $"{team1name} vs. {team2name}")
                    .AddField("event:", eventname)
                    .AddField("time:", dtDateTime.ToString().Substring(0, 16), true)
                    .AddField("format:", format, true)
                    .AddField("details:", $"[click here for more details]({link})");
            } else if(jArr.Count > 1)
            {
                int i = 0;
                foreach (JToken jTok in jArr)
                {
                    if (i == 2) { break; }
                    JObject jObj = JObject.Parse(jTok.ToString());
                    string team1name = JObject.Parse(JObject.Parse(jObj.GetValue("team1").ToString()).ToString()).GetValue("name").ToString();
                    if (team1name == "") { team1name = "TBD"; }
                    string team2name = JObject.Parse(JObject.Parse(jObj.GetValue("team2").ToString()).ToString()).GetValue("name").ToString();
                    if (team2name == "") { team2name = "TBD"; }
                    string format = jObj.GetValue("format").ToString();
                    string eventname = JObject.Parse(JObject.Parse(jObj.GetValue("event").ToString()).ToString()).GetValue("name").ToString();
                    if (eventname == "") { eventname = "n.A"; }
                    string link;
                    if (team1name != "" && team2name != "" && eventname != "")
                    {
                        link = $"https://www.hltv.org/matches/{jObj.GetValue("id")}/{team1name.Replace(' ', '-')}-vs-" +
                        $"{team2name.Replace(' ', '-')}-{eventname.Replace(' ', '-')}";
                    }
                    else
                    {
                        link = "n.A";
                    }

                    double time = double.Parse(JObject.Parse(jObj.ToString()).GetValue("date").ToString());
                    DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dtDateTime = dtDateTime.AddMilliseconds(time);

                    builder.AddField("match:", $"{team1name} vs. {team2name}", true)
                        .AddField("time:", dtDateTime.ToString().Substring(0, 16), true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("event:", eventname, true)
                        .AddField("format:", format, true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("details:", $"[click here for more details]({link})");
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
                double time = double.Parse(JObject.Parse(jTok.ToString()).GetValue("date").ToString());
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddMilliseconds(time);
                if (dtDateTime.CompareTo(DateTime.Now) != -1)
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
                string eventname;
                string team1name;
                string team2name;

                if (JObject.Parse(jTok.ToString()).GetValue("event") == null) { eventname = "n.A"; }
                else { eventname = JObject.Parse(JObject.Parse(jTok.ToString()).GetValue("event").ToString()).GetValue("name").ToString().ToLower(); }
                if (JObject.Parse(jTok.ToString()).GetValue("team1") == null) { team1name = "n.A"; }
                else { team1name = JObject.Parse(JObject.Parse(jTok.ToString()).GetValue("team1").ToString()).GetValue("name").ToString().ToLower(); }
                if(JObject.Parse(jTok.ToString()).GetValue("team2") == null) { team2name = "n.A"; }
                else { team2name = JObject.Parse(JObject.Parse(jTok.ToString()).GetValue("team2").ToString()).GetValue("name").ToString().ToLower(); }

                double time = double.Parse(JObject.Parse(jTok.ToString()).GetValue("date").ToString());
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddMilliseconds(time);
                if (arg.ToLower() == eventname || arg.ToLower() == team1name || arg.ToLower() == team2name)
                {
                    if (dtDateTime.CompareTo(DateTime.Now) != -1) { result.Add(jTok); }                    
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
                double time = double.Parse(JObject.Parse(jTok.ToString()).GetValue("date").ToString());
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddMilliseconds(time);
                if (dtDateTime.ToString().Substring(0, 10) == date.ToString().Substring(0, 10))
                {
                    if (dtDateTime.CompareTo(DateTime.Now) != -1) { result.Add(jTok); }
                }                
            }
            return result;
        }
    }
}
