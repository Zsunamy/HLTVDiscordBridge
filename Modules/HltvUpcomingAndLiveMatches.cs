using Discord;
using Discord.Commands;
using Discord.Rest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvUpcomingAndLiveMatches : ModuleBase<SocketCommandContext>
    {
        #region API
        public static async Task<(JArray, JArray)> AktUpcomingAndLiveMatches()
        {
            Directory.CreateDirectory("./cache/matches");
            var req = await Tools.RequestApiJArray("getMatches", new List<string>(), new List<string>());
            if(!req.Item2) { return (null, null); }
            JArray jArr = req.Item1;
            JArray upcomingMatches = new();
            JArray liveMatches = new();
            foreach(JObject jObj in jArr)
            {
                if(bool.Parse(jObj.GetValue("live").ToString())) { liveMatches.Add(jObj); } 
                else { upcomingMatches.Add(jObj); }
            }
            File.WriteAllText("./cache/matches/upcomingMatches.json", upcomingMatches.ToString());
            File.WriteAllText("./cache/matches/liveMatches.json", liveMatches.ToString());            
            StatsUpdater.StatsTracker.LiveMatches = liveMatches.Count;
            StatsUpdater.UpdateStats();
            return (upcomingMatches, liveMatches);
        }
        #endregion

        #region Embeds
        private static Embed BuildUpcomingMatchesEmbed(string arg, SocketCommandContext Context)
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

            if(jArr.Count == 0) { builder.WithDescription("there are no upcoming matches"); }

            foreach(JObject jObj in jArr)
            {
                if(jArr.IndexOf(jObj) > 1) { break; }
                string team1name;
                string team1link = "";
                string team2name;
                string team2link = "";
                string eventname;
                string eventlink = "";
                if(jObj.TryGetValue("team1", out JToken team1Tok))
                {
                    JObject team1 = JObject.Parse(team1Tok.ToString());
                    team1name = team1.GetValue("name").ToString();
                    team1link = $"https://www.hltv.org/team/{team1.GetValue("id")}/{team1name.ToLower().Replace(" ", "-")}";
                }
                else { team1name = "TBD"; }

                if(jObj.TryGetValue("team2", out JToken team2Tok))
                {
                    JObject team2 = JObject.Parse(team2Tok.ToString());
                    team2name = team2.GetValue("name").ToString();
                    team2link = $"https://www.hltv.org/team/{team2.GetValue("id")}/{team2name.ToLower().Replace(" ", "-")}";
                }
                else { team2name = "TBD"; }                
                string format = jObj.GetValue("format").ToString();
                if(jObj.TryGetValue("event", out JToken eventTok))
                {
                    JObject eventObj = JObject.Parse(eventTok.ToString());
                    eventname = eventObj.GetValue("name").ToString();
                    eventlink = $"https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventname.ToLower().Replace(" ", "-")}";
                } else
                {
                    eventname = "n.A";
                }
                
                string link = $"https://www.hltv.org/matches/{jObj.GetValue("id")}/{team1name.Replace(' ', '-')}-vs-" +
                    $"{team2name.Replace(' ', '-')}-{eventname.Replace(' ', '-')}";                

                builder.AddField("match:", $"[{team1name}]({team1link}) vs. [{team2name}]({team2link})", true);

                JToken dateTok = JObject.Parse(jObj.ToString()).GetValue("date");
                if (dateTok != null)
                {
                    double time = double.Parse(JObject.Parse(jObj.ToString()).GetValue("date").ToString());
                    DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dtDateTime = dtDateTime.AddMilliseconds(time);
                    builder.AddField("time:", dtDateTime.ToString().Substring(0, 16) + " UTC", true);
                }
                if (bool.Parse(JObject.Parse(jObj.ToString()).GetValue("live").ToString()))
                {
                    builder.AddField("time:", "now live!", true);
                }
                else if (dateTok == null && !bool.Parse(JObject.Parse(jObj.ToString()).GetValue("live").ToString()))
                {
                    builder.AddField("time:", "n.A", true);
                }

                builder.AddField("\u200b", "\u200b", true)
                    .AddField("event:", $"[{eventname}]({eventlink})", true)
                    .AddField("format:", GetFormatFromAcronym(format), true)
                    .AddField("\u200b", "\u200b", true)
                    .AddField("details:", $"[click here for more details]({link})");
                if(jArr.IndexOf(jObj) == 0)
                {
                    builder.AddField("\u200b", "\u200b");
                }                    
            }

            builder.WithCurrentTimestamp()
                .WithColor(Color.Blue)
                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            return builder.Build();
        }

        private static async Task<(Embed, ushort)> GetLiveMatchesEmbed()
        {
            //JArray matches = (await AktUpcomingAndLiveMatches()).Item2;
            File.WriteAllText("./cache/text.json",new JArray().ToString());
            JArray matches = JArray.Parse(File.ReadAllText("./cache/text.json"));

            EmbedBuilder builder = new();
            if (matches == null)
            {
                builder.WithColor(Color.Red)
                   .WithTitle($"error")
                   .WithDescription("Our API is currently not available! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues). We're sorry for the inconvience")
                   .WithCurrentTimestamp();
                return (builder.Build(), 0);
            }
            builder.WithTitle("LIVE MATCHES")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();
            foreach (JObject jObj in matches)
            {
                Emoji emote = new((matches.IndexOf(jObj) + 1).ToString() + "️⃣");
                JObject team1 = JObject.Parse(jObj.GetValue("team1").ToString());
                JObject team2 = JObject.Parse(jObj.GetValue("team2").ToString());
                JObject eventObj = JObject.Parse(jObj.GetValue("event").ToString());                
                string matchpageLink = $"https://www.hltv.org/matches/{jObj.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-')}-vs-{team2.GetValue("name").ToString().Replace(' ', '-')}-" +
                    $"{eventObj.GetValue("name").ToString().Replace(' ', '-')}";
                string eventLink = $"https://hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')}";
                builder.AddField($"{emote} {team1.GetValue("name")} vs. {team2.GetValue("name")}",
                    $"[matchpage]({matchpageLink})\n" +
                    $"event: [{eventObj.GetValue("name")}]({eventLink})\n");
#if DEBUG
                builder.WithFooter("React with the matchnumber to add a live scoreboard to your server!");
#endif
            }
            return (builder.Build(), ushort.Parse(matches.Count.ToString()));
        }
        #endregion

        #region tools
        public static JArray SearchUpcoming()
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/matches/upcomingMatches.json"));
            JArray result = JArray.Parse("[]");
            foreach (JToken jTok in jArr)
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
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/matches/upcomingMatches.json"));
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
                if (JObject.Parse(jTok.ToString()).GetValue("team2") == null) { team2name = "n.A"; }
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
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/matches/upcomingMatches.json"));
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
        #endregion

        #region Commands
        [Command("live"), Alias("stream", "streams")]
        public async Task DisplayLiveMatches()
        {
            EmbedBuilder builder = new();
            builder.WithTitle("Your request is loading!")
                   .WithDescription("This may take up to 30 seconds")
                   .WithCurrentTimestamp();
            var LoadingMsg = await Context.Channel.SendMessageAsync(embed: builder.Build());
            IDisposable typingState = Context.Channel.EnterTypingState();
            (Embed, ushort) res = await GetLiveMatchesEmbed();
            typingState.Dispose();
            await LoadingMsg.DeleteAsync();
            StatsUpdater.StatsTracker.MessagesSent += 1;
            StatsUpdater.UpdateStats();
            RestUserMessage msg = (RestUserMessage)await ReplyAsync(embed: res.Item1);
            for (int i = 1; i <= res.Item2; i++)
            {
                Emoji emote = new(i.ToString() + "️⃣");
#if DEBUG
                await msg.AddReactionAsync(emote);
#endif
            }
        }

        [Command("upcoming"), Alias("upcomingmatches", "matches")]
        public async Task GetUpcoming([Remainder] string arg = "")
        {
            //Ausgabe nach Team oder Event oder Tag
            StatsUpdater.StatsTracker.MessagesSent += 1;
            StatsUpdater.UpdateStats();
            await ReplyAsync(embed: BuildUpcomingMatchesEmbed(arg, Context));
        }
        #endregion
    }
}
