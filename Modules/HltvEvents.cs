using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvEvents : ModuleBase<SocketCommandContext>
    {        
        public static async Task AktEvents(List<SocketTextChannel> channels) 
        {
            await UpdateUpcomingEvents();
            var res = await GetEventEmbed();
            if(res.Item1 != null)
            {
                foreach (SocketTextChannel channel in channels)
                {
                    try { await channel.SendMessageAsync(embed: res.Item1); }
                    catch (Discord.Net.HttpException) { Console.WriteLine($"not enough permission in channel {channel}"); continue; }   
                }
            }            
        }

        /// <summary>
        /// Gets new upcoming events and writes them into ./cache/events/upcoming.json
        /// </summary>
        /// <returns>new upcoming event as JObject</returns>
        private static async Task<(JObject, bool)> GetOngoingEvents() 
        {
            var URI = new Uri($"https://hltv-api-steel.vercel.app/api/ongoingevents");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            if(!File.Exists("./cache/events/ongoing.json"))
            {
                File.WriteAllText("./cache/events/ongoing.json", JArray.Parse(await httpResponse.Content.ReadAsStringAsync()).ToString());
                return (null, false);
            } else
            {
                JArray oldOngoing = JArray.Parse(File.ReadAllText("./cache/events/ongoing.json"));
                JArray OngoingEvents = JArray.Parse(await httpResponse.Content.ReadAsStringAsync());

                //get started events
                List<JToken> jTokens = new List<JToken>(OngoingEvents);
                foreach(JToken jTok in oldOngoing)
                {
                    foreach(JToken kTok in OngoingEvents)
                    {
                        if(jTok.ToString() == kTok.ToString()) { jTokens.Remove(kTok); break; }
                    }
                }
                if (jTokens.Count > 0)
                {
                    File.WriteAllText("./cache/events/ongoing.json", OngoingEvents.ToString());
                    if (DateTime.Now.Hour == 0) { return (JObject.Parse(jTokens[0].ToString()), true); }
                }

                //get ended events
                jTokens = new List<JToken>(oldOngoing);
                foreach (JToken jTok in OngoingEvents)
                {
                    foreach (JToken kTok in oldOngoing)
                    {
                        if (jTok.ToString() == kTok.ToString()) { jTokens.Remove(kTok); break; }
                    }
                }
                if(jTokens.Count > 0) 
                {
                    File.WriteAllText("./cache/events/ongoing.json", OngoingEvents.ToString());
                    return (JObject.Parse(jTokens[0].ToString()), false);
                }

                return (null, false);
            }
        }

        private static async Task<(Embed, bool)> GetEventEmbed()
        {
            var req = await GetOngoingEvents();
            JObject eventObj = req.Item1;
            EmbedBuilder builder = new EmbedBuilder();
            if (eventObj == null) { return (null, false); }
            JObject eventStats = await GetEventStats(ushort.Parse(eventObj.GetValue("id").ToString()));
            if (eventStats == null) { return (null, false); }

            JObject location = JObject.Parse(eventStats.GetValue("location").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventStats.GetValue("name").ToString().Replace(' ', '-')}";
            if (req.Item2 == true)
            {
                builder.WithTitle($"{eventStats.GetValue("name")} started!");
            }
            else
            {
                builder.WithTitle($"{eventStats.GetValue("name")} just ended!");
            }

            builder.AddField("starting:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToString().Substring(0, 16) + " UTC", true)
                .AddField("ending:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToString().Substring(0, 16) + " UTC", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("prize pool:", eventStats.GetValue("prizePool"), true)
                .AddField("location:", location.GetValue("name"), true)
                .AddField("\u200b", "\u200b", true);
            if (req.Item2 == true)
            {
                JArray teams = JArray.Parse(eventStats.GetValue("teams").ToString());
                string teamsString = "";
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        string teamLink = $"https://www.hltv.org/team/{JObject.Parse(teams[i].ToString()).GetValue("id")}/{JObject.Parse(teams[i].ToString()).GetValue("name").ToString().Replace(' ', '-')}";
                        teamsString += $"[{JObject.Parse(teams[i].ToString()).GetValue("name")}]({teamLink})\n";
                    }
                    catch (IndexOutOfRangeException) { break; }
                    if (i == 4) { teamsString += $"and {teams.Count - 5} more"; }
                }
                builder.AddField("teams:", teamsString);
            }
            else
            {
                JArray prizeDistribution = JArray.Parse(eventStats.GetValue("prizeDistribution").ToString());
                if (prizeDistribution.ToString() != "[]")
                {
                    string prizeString = "";
                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            string teamLink = $"https://www.hltv.org/team/{JObject.Parse(JObject.Parse(prizeDistribution[i].ToString()).GetValue("team").ToString()).GetValue("id")}/" +
                                $"{JObject.Parse(JObject.Parse(prizeDistribution[i].ToString()).GetValue("team").ToString()).GetValue("name").ToString().Replace(' ', '-')}";
                            prizeString += $"{JObject.Parse(prizeDistribution[i].ToString()).GetValue("place")} " +
                              $"[{JObject.Parse(JObject.Parse(prizeDistribution[i].ToString()).GetValue("team").ToString()).GetValue("name")}]({teamLink}) " +
                              $"({JObject.Parse(prizeDistribution[i].ToString()).GetValue("prize")})" + "\n";
                        }
                        catch (IndexOutOfRangeException) { prizeString = "\u200b"; break; }
                        catch (NullReferenceException) { prizeString = "\u200b"; break; }
                        if (i == 4) { prizeString += $"and {prizeDistribution.Count - 4} more"; }
                    }
                    builder.AddField("results:", prizeString);
                }
            }

            builder.WithColor(Color.Green)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventLink)
                .WithCurrentTimestamp();
            bool featured = bool.Parse(eventObj.GetValue("featured").ToString());
            if (featured) { builder.WithFooter("This is a featured event"); }
            else { builder.WithFooter("This event is not featured"); }

            return (builder.Build(), featured);
        }

        /// <summary>
        /// Gets detailed eventstats by its eventID
        /// </summary>
        /// <param name="eventId">eventId</param>
        /// <returns>JObject with stats of the event</returns>
        private static async Task<JObject> GetEventStats(ushort eventId)
        {
            var URI = new Uri($"https://hltv-api-steel.vercel.app/api/eventbyid/{eventId}");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JObject jObj;
            try { jObj = JObject.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return null; }
            return jObj;
        }
        private static async Task<JObject> GetEventStats(string eventName)
        {
            var URI = new Uri($"https://hltv-api-steel.vercel.app/api/event/{eventName}");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JObject jObj;
            try { jObj = JObject.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return null; }
            return jObj;
        }
        private static async Task UpdateUpcomingEvents()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/upcommingevents");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr;
            try { jArr = JArray.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return; }

            Directory.CreateDirectory("./cache/events");
            File.WriteAllText("./cache/events/upcoming.json", jArr.ToString());
        }
        private static DateTime UnixTimeStampToDateTime(string unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(double.Parse(unixTimeStamp)).ToUniversalTime();
            dtDateTime = dtDateTime.AddHours(1);
            return dtDateTime;
        }

        #region COMMANDS
        [Command("events")] 
        public async Task GetAllOngoingEvents()
        {
            EmbedBuilder builder = new EmbedBuilder();
            JArray events = JArray.Parse(File.ReadAllText("./cache/events/ongoing.json"));
            string eventString = "";
            foreach(JToken jTok in events)
            {
                JObject eventObj = JObject.Parse(jTok.ToString());
                string toAdd = $"[{eventObj.GetValue("name")}](https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')})\n";
                eventString += toAdd;
                if(eventString.Length > 850)
                {                   
                    eventString = eventString.Remove(eventString.Length - toAdd.Length);
                    eventString += $"and {events.Count - events.IndexOf(jTok) - 1} more";
                    break;
                }
            }
            builder.WithTitle("ONGOING EVENTS")
                .WithColor(Color.Green)
                .AddField("events:", eventString)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://www.hltv.org/events#tab-ALL")
                .WithCurrentTimestamp()
                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
        }

        [Command("upcomingevents")]
        public async Task GetAllUpcomingEvents()
        {
            int i = 0;
            EmbedBuilder builder = new EmbedBuilder();
            JArray events = JArray.Parse(JObject.Parse(JArray.Parse(File.ReadAllText("./cache/events/upcoming.json"))[0].ToString()).GetValue("events").ToString());
            JArray eventsMonth2 = JArray.Parse(JObject.Parse(JArray.Parse(File.ReadAllText("./cache/events/upcoming.json"))[1].ToString()).GetValue("events").ToString());
            foreach (JToken jTok in eventsMonth2) { events.Add(jTok); }
            string eventString = "";
            foreach (JToken jTok in events)
            {
               if(i == 7) { eventString += $"and {events.Count - 6} more in the next 30 days"; break; }
                JObject eventObj = JObject.Parse(jTok.ToString());
                DateTime date = UnixTimeStampToDateTime(eventObj.GetValue("dateStart").ToString());
                if (date.AddDays(30).CompareTo(DateTime.Now) == -1) { break; }
                eventString += $"[{eventObj.GetValue("name")}](https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')}) " +
                    $"({date.ToString().Substring(0, 10)})\n";
                i++;
            }
            builder.WithTitle("UPCOMING EVENTS")
                .WithColor(Color.Green)
                .AddField("events:", eventString)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://www.hltv.org/events#tab-ALL")
                .WithCurrentTimestamp()
                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
        }

        //[Command("event")]
        public async Task GetEventByName([Remainder]string arg = "")
        {
            Config _cfg = new Config();
            EmbedBuilder builder = new EmbedBuilder();
            string prefix;
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel))) { prefix = "!"; } 
            else { prefix = _cfg.GetServerConfig(Context.Guild).Prefix; }            
            if(arg == "")
            {
                builder.WithColor(Color.Red)
                    .WithTitle("SYNTAX ERROR")
                    .WithCurrentTimestamp();                
                builder.WithDescription($"Please mind the syntax: {prefix}event [eventname]");
                    
                await ReplyAsync(embed: builder.Build());
                return;
            }
            JObject eventStats = await GetEventStats(arg);
            if(eventStats == null) { return; }
            JObject location = JObject.Parse(eventStats.GetValue("location").ToString());
            builder.WithTitle($"{eventStats.GetValue("name")}")
                .AddField("starting:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToString().Substring(0, 16) + " UTC", true)
                .AddField("ending:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToString().Substring(0, 16) + " UTC", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("prize pool:", eventStats.GetValue("prizePool"), true)
                .AddField("location:", location.GetValue("name"), true)
                .AddField("\u200b", "\u200b", true);
            if(UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).CompareTo(DateTime.Now) > 0)
            {
                Console.WriteLine("future" + eventStats.GetValue("name"));
            } else
            {
                Console.WriteLine("past" + eventStats.GetValue("name"));
            }
        }
        #endregion

    }
}
