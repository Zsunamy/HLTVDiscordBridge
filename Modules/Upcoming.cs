using Discord;
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
        public static async Task UpdateUpcomingMatches()
        {
            //var URI = new Uri("https://hltv-api-steel.vercel.app/api/matches");
            var URI = new Uri("http://revilum.com:3000/api/matches");
            HttpClient http = new();
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpResult = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr;
            try { jArr = JArray.Parse(httpResult); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return; }
            Directory.CreateDirectory("./cache");
            if (!File.Exists("./cache/upcoming.json"))
            {
                FileStream fs = File.Create("./cache/upcoming.json");
                fs.Close();
                File.WriteAllText("./cache/upcoming.json", jArr.ToString());
                return;
            }
            File.WriteAllText("./cache/upcoming.json", jArr.ToString());
        }        
        private static Embed BuildEmbed(string arg, SocketCommandContext Context)
        {
            JArray jArr;
            EmbedBuilder builder = new();
            if (DateTime.TryParse(arg, out DateTime date))
            {
                jArr = SearchUpcoming(date);
                builder.WithTitle($"UPCOMING MATCHES FOR {date.Date.ToString().Substring(0, 10)}");
            }
            else if (arg == "") { builder.WithTitle($"UPCOMING MATCHES"); jArr = SearchUpcoming(); }
            else { builder.WithTitle($"UPCOMING MATCHES FOR {arg.ToUpper()}"); jArr = SearchUpcoming(arg); }

            if (jArr.Count == 0)
            {
                builder.WithDescription("there are no upcoming matches");
            } 
            else if(jArr.Count == 1) 
            {
                JObject jObj = JObject.Parse(jArr[0].ToString());
                string team1name;
                string team1link = "";
                string team2name;
                string team2link = "";
                string eventname;
                string eventlink;
                try {
                    JObject team1 = JObject.Parse(JObject.Parse(jObj.GetValue("team1").ToString()).ToString());
                    team1name = team1.GetValue("name").ToString();
                    team1link = $"https://www.hltv.org/team/{team1.GetValue("id")}/{team1name.ToLower().Replace(" ", "-")}";
                }
                catch(NullReferenceException) { team1name = "TBD"; }
                try {
                    JObject team2 = JObject.Parse(JObject.Parse(jObj.GetValue("team2").ToString()).ToString());
                    team2name = team2.GetValue("name").ToString();
                    team2link = $"https://www.hltv.org/team/{team2.GetValue("id")}/{team2name.ToLower().Replace(" ", "-")}";
                }
                catch(NullReferenceException) { team2name = "TBD"; }
                string format = jObj.GetValue("format").ToString();                
                try {
                    JObject eventObj = JObject.Parse(JObject.Parse(jObj.GetValue("event").ToString()).ToString());
                    eventname = eventObj.GetValue("name").ToString();
                    eventlink = $"https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventname.ToLower().Replace(" ", "-")}";
                }
                catch (NullReferenceException) { eventname = "n.A"; }
                string link;
                if (team1name != "" && team2name != "" && eventname != "")
                {
                    link = $"https://www.hltv.org/matches/{jObj.GetValue("id")}/{team1name.Replace(' ', '-')}-vs-" +
                    $"{team2name.Replace(' ', '-')}-{eventname.Replace(' ', '-')}";
                } else
                {
                    link = "n.A";
                }

                builder.AddField("match:", $"[{team1name}]({team1link}) vs. [{team2name}]({team2link})", true);
                JToken dateTok = JObject.Parse(jObj.ToString()).GetValue("date");
                if (dateTok != null)
                {
                    double time = double.Parse(JObject.Parse(jObj.ToString()).GetValue("date").ToString());
                    DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dtDateTime = dtDateTime.AddMilliseconds(time);
                    builder.AddField("time:", dtDateTime.ToString().Substring(0, 16) + " UTC", true);
                }
                if(bool.Parse(JObject.Parse(jObj.ToString()).GetValue("live").ToString()))
                {
                    builder.AddField("time:", "now live!", true);
                }
                else if(dateTok == null && !bool.Parse(JObject.Parse(jObj.ToString()).GetValue("live").ToString()))
                {
                    builder.AddField("time:", "n.A", true);
                }
                builder.AddField("\u200b", "\u200b", true)
                    .AddField("event:", eventname, true)
                    .AddField("format:", GetFormatFromAcronym(format), true)
                    .AddField("\u200b", "\u200b", true)
                    .AddField("details:", $"[click here for more details]({link})");
            } 
            else if(jArr.Count > 1)
            {
                int i = 0;
                foreach (JObject jObj in jArr)
                {
                    if (i == 2) { break; }
                    string team1name;
                    string team1link = "";
                    string team2name;
                    string team2link = "";
                    string eventname;
                    string eventlink = "";
                    try
                    {
                        JObject team1 = JObject.Parse(JObject.Parse(jObj.GetValue("team1").ToString()).ToString());
                        team1name = team1.GetValue("name").ToString();
                        team1link = $"https://www.hltv.org/team/{team1.GetValue("id")}/{team1name.ToLower().Replace(" ", "-")}";
                    }
                    catch (NullReferenceException) { team1name = "TBD"; }
                    try
                    {
                        JObject team2 = JObject.Parse(JObject.Parse(jObj.GetValue("team2").ToString()).ToString());
                        team2name = team2.GetValue("name").ToString();
                        team2link = $"https://www.hltv.org/team/{team2.GetValue("id")}/{team2name.ToLower().Replace(" ", "-")}";
                    }
                    catch (NullReferenceException) { team2name = "TBD"; }
                    string format = jObj.GetValue("format").ToString();
                    try {
                        JObject eventObj = JObject.Parse(JObject.Parse(jObj.GetValue("event").ToString()).ToString());
                        eventname = eventObj.GetValue("name").ToString();
                        eventlink = $"https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventname.ToLower().Replace(" ", "-")}";
                    }
                    catch (NullReferenceException) { eventname = "n.A"; }
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

                    builder.AddField("match:", $"[{team1name}]({team1link}) vs. [{team2name}]({team2link})", true);

                    JToken dateTok = JObject.Parse(jObj.ToString()).GetValue("date");
                    if(dateTok != null)
                    {
                        double time = double.Parse(JObject.Parse(jObj.ToString()).GetValue("date").ToString());
                        DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        dtDateTime = dtDateTime.AddMilliseconds(time);
                        builder.AddField("time:", dtDateTime.ToString().Substring(0, 16) + " UTC", true);
                    } 
                    if(bool.Parse(JObject.Parse(jObj.ToString()).GetValue("live").ToString()))
                    {
                        builder.AddField("time:", "now live!", true);
                    }
                    else if(dateTok == null && !bool.Parse(JObject.Parse(jObj.ToString()).GetValue("live").ToString()))
                    {
                        builder.AddField("time:", "n.A", true);
                    }
                    

                    
                        
                    builder.AddField("\u200b", "\u200b", true)
                        .AddField("event:", $"[{eventname}]({eventlink})", true)
                        .AddField("format:", GetFormatFromAcronym(format), true)
                        .AddField("\u200b", "\u200b", true)
                        .AddField("details:", $"[click here for more details]({link})");
                    if (i == 0) {  }
                    i++;
                }
                
                builder.WithFooter($"and {jArr.Count - 2} more");
            }

            builder.WithCurrentTimestamp()
                .WithColor(Color.Blue)
                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            return builder.Build();
        }
        public static JArray SearchUpcoming()
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            JArray result = JArray.Parse("[]");
            foreach(JToken jTok in jArr)
            {
                JToken date = JObject.Parse(jTok.ToString()).GetValue("date");
                if (date == null) { result.Add(jTok); continue; }

                double time = double.Parse(date.ToString());
                DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddMilliseconds(time);
                if (dtDateTime.CompareTo(DateTime.Now.ToUniversalTime()) != -1)
                {
                    result.Add(jTok);
                }
            }
            return result;
        }
        public static JArray SearchUpcoming(string arg)
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

                JToken date = JObject.Parse(jTok.ToString()).GetValue("date");
                DateTime dtDateTime;
                if (date != null) 
                {
                    double time = double.Parse(date.ToString());
                    dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dtDateTime = dtDateTime.AddMilliseconds(time);
                }
                else { dtDateTime = new DateTime(2035, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc); }

                
                if (arg.ToLower() == eventname || arg.ToLower() == team1name || arg.ToLower() == team2name)
                {
                    if (dtDateTime.CompareTo(DateTime.Now.ToUniversalTime()) != -1) { result.Add(jTok); }                    
                }
            }
            return result;
        }
        public static JArray SearchUpcoming(DateTime dateArg)
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            JArray result = JArray.Parse("[]");
            foreach (JToken jTok in jArr)
            {
                JToken date = JObject.Parse(jTok.ToString()).GetValue("date");
                if (date == null) { result.Add(jTok); continue; }

                double time = double.Parse(date.ToString());

                DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddMilliseconds(time);
                if (dtDateTime.ToString().Substring(0, 10) == dateArg.ToString().Substring(0, 10))
                {
                    if (dtDateTime.CompareTo(DateTime.Now.ToUniversalTime()) != -1) { result.Add(jTok); }
                }                
            }
            return result;
        }
        private static string GetFormatFromAcronym(string req)
        {
            return req switch
            {
                "bo1" => "Best of 1",
                "bo3" => "Best of 3",
                "bo5" => "Best of 5",
                "bo7" => "Best of 7",
                _ => "n.A",
            };
        }

        [Command("upcoming")]
        public async Task GetUpcoming([Remainder] string arg = "")
        {
            //Ausgabe nach Team oder Event oder Tag
            await ReplyAsync(embed: BuildEmbed(arg, Context));
        }
    }
}
