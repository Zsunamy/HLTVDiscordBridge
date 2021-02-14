﻿using Discord;
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
        public async Task AktEvents(List<SocketTextChannel> channels) 
        {
            Config _cfg = new Config();
            var res = await GetOngoingEventEmbed(await GetOngoingEvents());
            if(res.Item1 != null)
            {
                foreach (SocketTextChannel channel in channels)
                {   
                    if(!_cfg.GetServerConfig(channel).OnlyFeaturedEvents || _cfg.GetServerConfig(channel).OnlyFeaturedEvents == res.Item2)
                    {
                        await channel.SendMessageAsync("", false, res.Item1);
                    }
                }
            }            
        }

        /// <summary>
        /// Gets new upcoming events and writes them into ./cache/events/upcoming.json
        /// </summary>
        /// <returns>new upcoming event as JObject</returns>
        public async Task<JObject> GetUpcomingEvents() 
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/upcommingevents");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr = null;
            try { jArr = JArray.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); }

            Directory.CreateDirectory("./cache/events");
            if (!File.Exists("./cache/events/upcoming.json"))
            {
                FileStream fs = File.Create("./cache/events/upcoming.json");
                fs.Close();
                File.WriteAllText("./cache/events/upcoming.json", jArr.ToString());                
                return null;
            }
            JArray cachedJArray = JArray.Parse(File.ReadAllText("./cache/events/upcoming.json"));
            if (cachedJArray != jArr)
            {                
                foreach (JToken jTok in jArr)
                {
                    bool update = true;
                    foreach (JToken jToken in cachedJArray)
                    {
                        if (jTok.ToString() == jToken.ToString()) { update = false; }
                    }
                    if (update)
                    {
                        File.WriteAllText("./cache/events/upcoming.json", jArr.ToString());
                        return JObject.Parse(jTok.ToString());
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all ongoing events and writes them into ./cache/events/ongoing.json
        /// </summary>
        /// <returns>JObject diff of api req and cached events. bool returns true for started events / returns false for ended events.</returns>
        public async Task<(JObject, bool)> GetOngoingEvents()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/ongoingevents");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr = null;
            try { jArr = JArray.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); }

            Directory.CreateDirectory("./cache/events");
            if (!File.Exists("./cache/events/ongoing.json"))
            {
                FileStream fs = File.Create("./cache/events/ongoing.json");
                fs.Close();
                File.WriteAllText("./cache/events/ongoing.json", jArr.ToString());
                return (null, false);
            }
            JArray cachedJArray = JArray.Parse(File.ReadAllText("./cache/events/ongoing.json"));
            if (cachedJArray != jArr)
            {
                //get ended event
                foreach (JToken jTok in cachedJArray)
                {
                    bool update = true;
                    foreach (JToken jToken in jArr)
                    {
                        if (jTok.ToString() == jToken.ToString()) { update = false; }
                    }
                    if (update)
                    {
                        File.WriteAllText("./cache/events/ongoing.json", jArr.ToString());
                        return (JObject.Parse(jTok.ToString()), false);
                    }
                }
                //get new started event
                foreach (JToken jTok in jArr)
                {
                    bool update = true;
                    foreach (JToken jToken in cachedJArray)
                    {
                        if (jTok.ToString() == jToken.ToString()) { update = false; }
                    }
                    if (update)
                    {
                        File.WriteAllText("./cache/events/ongoing.json", jArr.ToString());
                        return (JObject.Parse(jTok.ToString()), true);
                    }
                }
            }
            return (null, false);
        }

        /// <summary>
        /// Gets detailed eventstats by its eventID
        /// </summary>
        /// <param name="eventId">eventId</param>
        /// <returns>JObject with stats of the event</returns>
        public async Task<JObject> GetEventStats(ushort eventId)
        {
            var URI = new Uri($"https://hltv-api-steel.vercel.app/api/event/{eventId}");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JObject jObj = null;
            try { jObj = JObject.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return null; }
            return jObj;
        }

        /// <summary>
        /// Builds the Embed of started/ended events
        /// </summary>
        /// <param name="arg">JObject(eventdata) and bool(true = started; false = ended)</param>
        /// <returns>Embed, bool(true for a featured event)</returns>
        public async Task<(Embed, bool)> GetOngoingEventEmbed((JObject, bool) arg)
        {
            JObject eventObj = arg.Item1;
            EmbedBuilder builder = new EmbedBuilder();
            if (eventObj == null) { return (null, false); }
            JObject eventStats = await GetEventStats(ushort.Parse(eventObj.GetValue("id").ToString()));
            if (eventStats == null) { return (null, false); }
            
            JObject location = JObject.Parse(eventStats.GetValue("location").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventStats.GetValue("name").ToString().Replace(' ', '-')}";
            if(arg.Item2 == true)
            {
                builder.WithTitle($"{eventStats.GetValue("name")} started!");
            }
            else
            {
                builder.WithTitle($"{eventStats.GetValue("name")} just ended!");
            }
            
            builder.AddField("starting:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()/*.Substring(0, 16)*/) + " UTC", true)
                .AddField("ending:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()/*.Substring(0, 16)*/) + " UTC", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("prize pool:", eventStats.GetValue("prizePool"), true)
                .AddField("location:", location.GetValue("name"), true)
                .AddField("\u200b", "\u200b", true);
            if(arg.Item2 == true)
            {
                JArray teams = JArray.Parse(eventStats.GetValue("teams").ToString());
                string teamsString = "";
                for (int i = 0; i < 5; i++)
                {
                    try { teamsString += JObject.Parse(teams[i].ToString()).GetValue("name").ToString() + "\n"; }
                    catch (IndexOutOfRangeException) { break; }
                    if (i == 4) { teamsString += $"and {teams.Count - 5} more"; }
                }
                builder.AddField("teams:", teamsString);
            } else
            {
                JArray prizeDistribution = JArray.Parse(eventStats.GetValue("prizeDistribution").ToString());
                string prizeString = "";
                for (int i = 0; i < 4; i++)
                {
                    try { prizeString += $"{JObject.Parse(prizeDistribution[i].ToString()).GetValue("place")} " +
                            $"{JObject.Parse(JObject.Parse(prizeDistribution[i].ToString()).GetValue("team").ToString()).GetValue("name")} " +
                            $"({JObject.Parse(prizeDistribution[i].ToString()).GetValue("prize")})" + "\n"; }
                    catch (IndexOutOfRangeException) { break; }
                    if (i == 4) { prizeString += $"and {prizeDistribution.Count - 4} more"; }
                }
                builder.AddField("results:", prizeString);
            }

            builder.WithColor(Color.Green)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventLink)
                .WithCurrentTimestamp();

            return (builder.Build(), bool.Parse(eventObj.GetValue("featured").ToString()));
        }
        public static DateTime UnixTimeStampToDateTime(string unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(double.Parse(unixTimeStamp)).ToUniversalTime();
            return dtDateTime;
        }


        //User commands
        [Command("events")] 
        public async Task GetAllOngoingEvents()
        {
            EmbedBuilder builder = new EmbedBuilder();
            JArray events = JArray.Parse(File.ReadAllText("./cache/events/ongoing.json"));
            string eventString = "";
            foreach(JToken jTok in events)
            {
                JObject eventObj = JObject.Parse(jTok.ToString());
                eventString += $"[{eventObj.GetValue("name")}](https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')})\n";
            }
            builder.WithTitle("ONGOING EVENTS")
                .WithColor(Color.Green)
                .AddField("events:", eventString)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://www.hltv.org/events#tab-ALL")
                .WithCurrentTimestamp();
            await ReplyAsync("", false, builder.Build());
        }
        [Command("upcomingevents")]
        public async Task GetAllUpcomingEvents()
        {
            EmbedBuilder builder = new EmbedBuilder();
            JArray events = JArray.Parse(JObject.Parse(JArray.Parse(File.ReadAllText("./cache/events/upcoming.json"))[0].ToString()).GetValue("events").ToString());
            string eventString = "";
            foreach (JToken jTok in events)
            {
                JObject eventObj = JObject.Parse(jTok.ToString());
                eventString += $"[{eventObj.GetValue("name")}](https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')}) " +
                    $"({UnixTimeStampToDateTime(eventObj.GetValue("dateStart").ToString()).ToString().Substring(0, 10)})\n";
            }
            builder.WithTitle("UPCOMING EVENTS")
                .WithColor(Color.Green)
                .AddField("events:", eventString)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://www.hltv.org/events#tab-ALL")
                .WithCurrentTimestamp();
            await ReplyAsync("", false, builder.Build());
        }
        [Command("event")]
        public async Task GetEventByName([Remainder]string arg = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            if(arg == "")
            {
                builder.WithColor(Color.Red)
                    .WithTitle("SYNTAX ERROR")
                    .WithDescription("Please mind the syntax: !event [eventname]")
                    .WithCurrentTimestamp();
                await ReplyAsync("", false, builder.Build());
                return;
            }
            JArray events = JArray.Parse(File.ReadAllText("./cache/events/upcoming.json"));
            foreach(JToken jTok in events)
            {
                JArray allEvents = JArray.Parse(JObject.Parse(jTok.ToString()).GetValue("events").ToString());
                JArray ongoingEvents = JArray.Parse(File.ReadAllText("./cache/events/ongoing.json"));
                foreach(JToken tok in ongoingEvents) { allEvents.Add(tok); }
                foreach(JToken jToken in allEvents)
                {
                    JObject eventStats = JObject.Parse(jToken.ToString());
                    string eventName = eventStats.GetValue("name").ToString().ToLower();
                    if(eventName.Contains(arg.ToLower()))
                    {
                        string eventLink = $"https://hltv.org/events/{eventStats.GetValue("id")}/{eventName.Replace(' ', '-')}";
                        builder.WithTitle($"{eventStats.GetValue("name")}")
                            .WithColor(Color.Green)
                            .AddField("starting:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToString().Substring(0, 16) + " UTC", true)
                            .AddField("ending:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToString().Substring(0, 16) + " UTC", true)
                            .AddField("\u200b", "\u200b", true);
                        if(eventStats.GetValue("prizePool") != null)
                        {
                            builder.AddField("prize pool:", eventStats.GetValue("prizePool"), true)
                                .AddField("location:", JObject.Parse(eventStats.GetValue("location").ToString()).GetValue("name").ToString(), true)
                                .AddField("\u200b", "\u200b", true);
                        }  
                        builder.WithAuthor("click for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventLink);
                        await ReplyAsync("", false, builder.Build());
                        return;
                    }
                }
            }
            builder.WithTitle($"ERROR")
                .WithColor(Color.Red)
                .WithDescription($"{arg} was not found in any event title in the next {events.Count} months");
            await ReplyAsync("", false, builder.Build());
        }

    }
}