using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvEvents : ModuleBase<SocketCommandContext>
    {        
        public static async Task AktEvents(List<SocketTextChannel> channels) 
        {
            List<ushort> startedEvents = await GetStartedEvents();            
            if (startedEvents != null) 
            {
                foreach (ushort eventId in startedEvents)
                {
                    JObject eventStats = await GetEventStats(eventId);
                    if(eventStats != null)
                    {
                        foreach (SocketTextChannel channel in channels)
                        {
                            ServerConfig config = Config.GetServerConfig(channel);
                            if (config.EventOutput)
                            {
                                try { 
                                    await channel.SendMessageAsync(embed: GetEventStartedEmbed(eventStats));
                                    StatsUpdater.StatsTracker.MessagesSent += 1;
                                    StatsUpdater.UpdateStats();
                                }
                                catch (Discord.Net.HttpException) { Program.WriteLog($"not enough permission in channel {channel}"); continue; }
                            }
                        }
                    }
                }
            }   
            
            List<ushort> endedEventIds = await GetEndedEvent();  
            if(endedEventIds != null)
            {
                foreach(ushort eventId in endedEventIds)
                {
                    JObject eventStats = await GetEventStats(eventId);
                    if(eventStats != null)
                    {
                        foreach (SocketTextChannel channel in channels)
                        {
                            ServerConfig config = Config.GetServerConfig(channel);
                            if (config.EventOutput)
                            {
                                try { await channel.SendMessageAsync(embed: GetEventEndedEmbed(eventStats)); }
                                catch (Discord.Net.HttpException) { Program.WriteLog($"not enough permission in channel {channel}"); continue; }
                            }
                        }
                    }
                }
            }
        }

        #region API
        /// <summary>
        /// Updates ongoing and upcoming events if something has changed
        /// </summary>
        /// <returns>All ongoing and upcoming events as JArray</returns>
        private static async Task<JArray> UpdateEvents()
        {
            var req = await Tools.RequestApiJArray("events");
            if(!req.Item2) { return null; }
            JArray events = req.Item1;
            Directory.CreateDirectory("./cache/events");
            if (!File.Exists("./cache/events/events.json"))
            {
                File.WriteAllText("./cache/events/events.json", events.ToString());
                return events;
            }
            else
            {
                JArray oldEvents = JArray.Parse(File.ReadAllText("./cache/events/events.json"));
                if(oldEvents != events)
                {
                    File.WriteAllText("./cache/events/events.json", events.ToString());
                }
                return events;
            }
        }
        private static async Task<JArray> UpdatePastEvents() 
        {
            var req = await Tools.RequestApiJArray("pastevents");
            if(!req.Item2) { return null; }
            JArray events = req.Item1;
            Directory.CreateDirectory("./cache/events");
            if (!File.Exists("./cache/events/pastevents.json"))
            {
                File.WriteAllText("./cache/events/pastevents.json", events.ToString());
                return events;
            }
            else
            {
                JArray oldEvents = JArray.Parse(File.ReadAllText("./cache/events/pastevents.json"));
                if (oldEvents != events)
                {
                    File.WriteAllText("./cache/events/pastevents.json", events.ToString());
                }
                return events;
            }
        }

        /// <summary>
        /// Gets detailed eventstats by its eventID
        /// </summary>
        /// <param name="eventId">eventId</param>
        /// <returns>JObject with stats of the event</returns>
        private static async Task<JObject> GetEventStats(ushort eventId)
        {
            var req = await Tools.RequestApiJObject($"eventbyid/{eventId}");
            return req.Item1;
        }
        private static async Task<(JObject, bool)> GetEventStats(string eventName)
        {
            var req = await Tools.RequestApiJObject($"event/{eventName}");
            return req;
        }
        private static async Task<JArray> GetLatestResultsOfEvent(ushort eventId)
        {
            var req = await Tools.RequestApiJArray($"events/[{eventId}]");
            return req.Item1;
        }
        #endregion

        #region started events
        /// <summary>
        /// Gets new live events
        /// </summary>
        /// <returns>List of eventIds</returns>
        private static async Task<List<ushort>> GetStartedEvents() 
        {
            JArray oldEvents = JArray.Parse(File.ReadAllText("./cache/events/events.json"));
            JArray newEvents = await UpdateEvents();
            if(newEvents == null) { return null; }
            if (oldEvents.ToString() == newEvents.ToString()) { return null; }
            List<ushort> eventIds = new();

            foreach(JObject jObj in newEvents)
            {
                if (jObj.TryGetValue("location", out JToken _)) { break; }
                if(!File.Exists("./cache/events/StartedEventIds.txt")) { File.WriteAllText("./cache/events/StartedEventIds.txt", jObj.GetValue("id").ToString() + "\n"); }
                else
                {
                    string[] Ids = File.ReadAllLines("./cache/events/StartedEventIds.txt");
                    bool alreadysent = false;
                    foreach (string id in Ids)
                    {
                        if(id == jObj.GetValue("id").ToString()) { alreadysent = true; break; }
                    }
                    if(alreadysent) { continue; }
                    File.WriteAllText("./cache/events/StartedEventIds.txt", jObj.GetValue("id").ToString() + "\n" + File.ReadAllText("./cache/events/StartedEventIds.txt"));
                }

                bool eventStarted = true;
                foreach (JObject obj in oldEvents)
                {
                    if (obj.TryGetValue("location", out JToken _)) { break; }
                    if(jObj.ToString() == obj.ToString()) { eventStarted = false; break; }
                }
                if(eventStarted) { eventIds.Add(ushort.Parse(jObj.GetValue("id").ToString())); }
            }
            return eventIds;
        }

        private static Embed GetEventStartedEmbed(JObject eventStats)
        {
            EmbedBuilder builder = new();
            if (eventStats == null) { return null; }
            builder.WithTitle($"{eventStats.GetValue("name")} just started!");
            builder.AddField("startdate:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToShortDateString(), true);
            builder.AddField("enddate:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToShortDateString(), true);
            builder.AddField("\u200b", "\u200b", true);
            builder.AddField("prize pool:", eventStats.GetValue("prizePool"), true);
            JObject location = JObject.Parse(eventStats.GetValue("location").ToString());
            builder.AddField("location:", location.GetValue("name"), true);
            builder.AddField("\u200b", "\u200b", true);
            JArray team = JArray.Parse(eventStats.GetValue("teams").ToString());
            string teamString = "";
            foreach(JObject jObj in team)
            {
                if(team.IndexOf(jObj) > 4) { teamString += $"and {team.Count - 5} more"; break; }
                string teamLink = $"https://www.hltv.org/team/{jObj.GetValue("id")}/{jObj.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                teamString += $"[{jObj.GetValue("name")}]({teamLink})\n";
            }
            builder.AddField("teams:", teamString);
            builder.WithColor(Color.Gold);
            builder.WithThumbnailUrl(eventStats.GetValue("logo").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventStats.GetValue("id")}/{eventStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventLink);
            builder.WithCurrentTimestamp();
            return builder.Build();
        }
        #endregion

        #region ended events
        private static async Task<List<ushort>> GetEndedEvent()
        {
            JArray oldEvents = JArray.Parse(File.ReadAllText("./cache/events/pastevents.json"));
            JArray newEvents = await UpdatePastEvents();
            if(newEvents == null) { return null; }
            if (oldEvents.ToString() == newEvents.ToString()) { return null; }
            List<ushort> eventIds = new();
            foreach(JObject jObj in newEvents)
            {
                if (!File.Exists("./cache/events/EndedEventIds.txt")) { File.WriteAllText("./cache/events/EndedEventIds.txt", jObj.GetValue("id").ToString() + "\n"); }
                else
                {
                    string[] Ids = File.ReadAllLines("./cache/events/EndedEventIds.txt");
                    bool alreadysent = false;
                    foreach (string id in Ids)
                    {
                        if (id == jObj.GetValue("id").ToString()) { alreadysent = true; break; }
                    }
                    if (alreadysent) { continue; }
                    File.WriteAllText("./cache/events/EndedEventIds.txt", jObj.GetValue("id").ToString() + "\n" + File.ReadAllText("./cache/events/EndedEventIds.txt"));
                }

                bool eventEnded = true;
                foreach(JObject obj in oldEvents)
                {
                    if(jObj.ToString() == obj.ToString()) { eventEnded = false; break; }
                }
                if (eventEnded) { eventIds.Add(ushort.Parse(jObj.GetValue("id").ToString())); }
            }
            return eventIds;
        }
        private static Embed GetEventEndedEmbed(JObject eventStats)
        {
            EmbedBuilder builder = new();
            if (eventStats == null) { return null; }
            builder.WithTitle($"{eventStats.GetValue("name")} just ended!");
            builder.AddField("startdate:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToShortDateString(), true);
            builder.AddField("enddate:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToShortDateString(), true);
            builder.AddField("\u200b", "\u200b", true);
            builder.AddField("prize pool:", eventStats.GetValue("prizePool"), true);
            JObject location = JObject.Parse(eventStats.GetValue("location").ToString());
            builder.AddField("location:", location.GetValue("name"), true);
            builder.AddField("\u200b", "\u200b", true);
            JArray prizeDistribution = JArray.Parse(eventStats.GetValue("prizeDistribution").ToString());
            string prizeDistributionString = "";
            foreach (JObject jObj in prizeDistribution)
            {
                if (prizeDistribution.IndexOf(jObj) > 4) { prizeDistributionString += $"and {prizeDistribution.Count - 5} more"; break; }
                JObject team = JObject.Parse(jObj.GetValue("team").ToString());
                string teamLink = $"https://www.hltv.org/team/{team.GetValue("id")}/{team.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                if(jObj.TryGetValue("prize", out JToken _) && jObj.TryGetValue("qualifiesFor", out JToken qualifi))
                {
                    JObject qual = JObject.Parse(qualifi.ToString());
                    string qualLink = $"https://www.hltv.org/events/{qual.GetValue("id")}/{qual.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                    prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink}) wins: {jObj.GetValue("prize")} & qualifies for: [{qual.GetValue("name")}]({qualLink})\n";
                }
                else if(jObj.TryGetValue("prize", out JToken _)) {
                    prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink}) wins: {jObj.GetValue("prize")}\n";
                } else if(jObj.TryGetValue("qualifiesFor", out JToken quali)) {
                    JObject qual = JObject.Parse(quali.ToString());
                    string qualLink = $"https://www.hltv.org/events/{qual.GetValue("id")}/{qual.GetValue("name").ToString().ToLower().Replace(' ','-')}";
                    prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink}) qualifies for: [{qual.GetValue("name")}]({qualLink})\n";
                }
                else {
                    prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink})\n";
                }
                
            }
            builder.AddField("results:", prizeDistributionString);
            builder.WithColor(Color.Gold);
            builder.WithThumbnailUrl(eventStats.GetValue("logo").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventStats.GetValue("id")}/{eventStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventLink);
            builder.WithCurrentTimestamp();
            return builder.Build();
        }
        #endregion

        #region tools
        private static DateTime UnixTimeStampToDateTime(string unixTimeStamp)
        {
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(double.Parse(unixTimeStamp)).ToUniversalTime();
            dtDateTime = dtDateTime.AddHours(1);
            return dtDateTime;
        }
        #endregion

        #region COMMANDS
        [Command("events"), Alias("ongoingevents")] 
        public async Task GetAllOngoingEvents()
        {
            EmbedBuilder builder = new();
            JArray events = JArray.Parse(File.ReadAllText("./cache/events/events.json"));
            string eventString = "";
            List<JObject> ongoingEvents = new();
            foreach(JObject jObj in events)
            {
                if(!jObj.TryGetValue("location", out _)) { ongoingEvents.Add(jObj); }
            }
            foreach(JObject eventObj in ongoingEvents)
            {
                string toAdd = $"[{eventObj.GetValue("name")}](https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')})\n";
                eventString += toAdd;
                if(eventString.Length > 850)
                {                   
                    eventString = eventString.Remove(eventString.Length - toAdd.Length);
                    eventString += $"and {ongoingEvents.Count - ongoingEvents.IndexOf(eventObj) - 1} more";
                    break;
                }
            }
            builder.WithTitle("ONGOING EVENTS")
                .WithColor(Color.Gold)
                .AddField("events:", eventString)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://www.hltv.org/events#tab-ALL")
                .WithCurrentTimestamp()
                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
        }

        [Command("upcomingevents")]
        public async Task GetAllUpcomingEvents()
        {
            JArray events = JArray.Parse(File.ReadAllText("./cache/events/events.json"));
            List<JObject> upcomingEvents = new();
            foreach(JObject jObj in events)
            {
                if(jObj.TryGetValue("location", out _)) { upcomingEvents.Add(jObj); }
            }
            string eventString = "";
            EmbedBuilder builder = new();            
            foreach (JObject eventObj in upcomingEvents)
            {
                if(eventString.Length > 850) { eventString += $"and {events.Count - 6} more"; break; }

                DateTime date = UnixTimeStampToDateTime(eventObj.GetValue("dateStart").ToString());
                eventString += $"[{eventObj.GetValue("name")}](https://www.hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')}) " +
                    $"({date.ToString().Substring(0, 10)})\n";
            }
            builder.WithTitle("UPCOMING EVENTS")
                .WithColor(Color.Gold)
                .AddField("events:", eventString)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://www.hltv.org/events#tab-ALL")
                .WithCurrentTimestamp()
                .WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
        }

        [Command("event")]
        public async Task GetEventByName([Remainder]string arg = "")
        {
            EmbedBuilder builder = new();
            builder.WithTitle("error")
                .WithColor(Color.Red)
                .WithDescription("This command is currently not available. Sorry for the inconvenience.")
                .WithCurrentTimestamp();
            await ReplyAsync(embed: builder.Build());
            return;
            /*
            Config _cfg = new();
            EmbedBuilder builder = new();

            string prefix;
            if (Context.Channel.GetType().Equals(typeof(SocketDMChannel))) { prefix = "!"; } 
            else { prefix = _cfg.GetServerConfig(Context.Guild).Prefix; }            
            if(arg == "")
            {
                builder.WithColor(Color.Red)
                    .WithTitle("syntax error")
                    .WithCurrentTimestamp();                
                builder.WithDescription($"Please mind the syntax: {prefix}event [eventname]");
                    
                await ReplyAsync(embed: builder.Build());
                return;
            }

            builder.WithTitle("Your request is loading!")
                .WithDescription("This may take up to 30 seconds")
                .WithCurrentTimestamp();
            var msg = await Context.Channel.SendMessageAsync(embed: builder.Build());
            IDisposable typingState = Context.Channel.EnterTypingState();
            var req = await GetEventStats(arg);
            JObject eventStats = req.Item1;
            typingState.Dispose();
            await msg.DeleteAsync();
            
            if(eventStats == null && !req.Item2) 
            {
                builder.WithColor(Color.Red)
                    .WithTitle($"error")
                    .WithDescription("Our API is currently not available! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues). We're sorry for the inconvience")
                    .WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
                typingState.Dispose();
                return; 
            }
            else if(eventStats == null && req.Item2)
            {
                builder.WithColor(Color.Red)
                    .WithTitle("error")
                    .WithCurrentTimestamp();
                builder.WithDescription($"The event {arg} does not exist or is not scheduled yet.");
                await ReplyAsync(embed: builder.Build());
                typingState.Dispose();
                return;
            }
            JObject location = JObject.Parse(eventStats.GetValue("location").ToString());
            builder.WithTitle($"{eventStats.GetValue("name")}");
            if(UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).CompareTo(DateTime.Now) > 0 && UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).CompareTo(DateTime.Now) > 0)
            {
                builder.AddField("starting:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToString().Substring(0, 16) + " UTC", true)
                .AddField("ending:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToString().Substring(0, 16) + " UTC", true);
            } else if(UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).CompareTo(DateTime.Now) < 0 && UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).CompareTo(DateTime.Now) < 0)
            {
                builder.AddField("started:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToString().Substring(0, 16) + " UTC", true)
                .AddField("ended:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToString().Substring(0, 16) + " UTC", true);
            } else
            {
                builder.AddField("started:", UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).ToString().Substring(0, 16) + " UTC", true)
                .AddField("ending:", UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).ToString().Substring(0, 16) + " UTC", true);
            }
            
            builder.AddField("\u200b", "\u200b", true)
                .AddField("prize pool:", eventStats.GetValue("prizePool"), true)
                .AddField("location:", location.GetValue("name"), true)
                .AddField("\u200b", "\u200b", true);
            if(UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).CompareTo(DateTime.Now) > 0 && UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).CompareTo(DateTime.Now) > 0)
            {
                //teams
                JArray team = JArray.Parse(eventStats.GetValue("teams").ToString());
                string teamString = "";
                foreach (JObject jObj in team)
                {
                    if (team.IndexOf(jObj) > 4) { teamString += $"and {team.Count - 5} more"; break; }
                    string teamLink = $"https://www.hltv.org/team/{jObj.GetValue("id")}/{jObj.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                    teamString += $"[{jObj.GetValue("name")}]({teamLink})\n";
                }
                builder.AddField("teams:", teamString);
            } else if(UnixTimeStampToDateTime(eventStats.GetValue("dateEnd").ToString()).CompareTo(DateTime.Now) < 0 && UnixTimeStampToDateTime(eventStats.GetValue("dateStart").ToString()).CompareTo(DateTime.Now) < 0)
            {
                //teams
                JArray teams = JArray.Parse(eventStats.GetValue("teams").ToString());
                string teamString = "";
                foreach (JObject jObj in teams)
                {
                    if (teams.IndexOf(jObj) > 4) { teamString += $"and {teams.Count - 5} more"; break; }
                    string teamLink = $"https://www.hltv.org/team/{jObj.GetValue("id")}/{jObj.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                    teamString += $"[{jObj.GetValue("name")}]({teamLink})\n";
                }
                builder.AddField("teams:", teamString, true);
                //prize distribution
                JArray prizeDistribution = JArray.Parse(eventStats.GetValue("prizeDistribution").ToString());
                string prizeDistributionString = "";
                foreach (JObject jObj in prizeDistribution)
                {
                    if (prizeDistribution.IndexOf(jObj) > 4) { prizeDistributionString += $"and {prizeDistribution.Count - 5} more"; break; }
                    JObject team = JObject.Parse(jObj.GetValue("team").ToString());
                    string teamLink = $"https://www.hltv.org/team/{team.GetValue("id")}/{team.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                    if (jObj.TryGetValue("prize", out JToken _) && jObj.TryGetValue("qualifiesFor", out JToken qualifi))
                    {
                        JObject qual = JObject.Parse(qualifi.ToString());
                        string qualLink = $"https://www.hltv.org/events/{qual.GetValue("id")}/{qual.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                        prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink}) wins: {jObj.GetValue("prize")} & qualifies for: [{qual.GetValue("name")}]({qualLink})\n";
                    }
                    else if (jObj.TryGetValue("prize", out JToken _))
                    {
                        prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink}) wins: {jObj.GetValue("prize")}\n";
                    }
                    else if (jObj.TryGetValue("qualifiesFor", out JToken quali))
                    {
                        JObject qual = JObject.Parse(quali.ToString());
                        string qualLink = $"https://www.hltv.org/events/{qual.GetValue("id")}/{qual.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                        prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink}) qualifies for: [{qual.GetValue("name")}]({qualLink})\n";
                    }
                    else
                    {
                        prizeDistributionString += $"{jObj.GetValue("place")} [{team.GetValue("name")}]({teamLink})\n";
                    }
                }
                builder.AddField("results:", prizeDistributionString, true);
                builder.AddField("\u200b", "\u200b", true);
            }
            else
            {
                //teams
                JArray teams = JArray.Parse(eventStats.GetValue("teams").ToString());
                string teamString = "";
                foreach (JObject jObj in teams)
                {
                    if (teams.IndexOf(jObj) > 4) { teamString += $"and {teams.Count - 5} more"; break; }
                    string teamLink = $"https://www.hltv.org/team/{jObj.GetValue("id")}/{jObj.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                    teamString += $"[{jObj.GetValue("name")}]({teamLink})\n";
                }
                builder.AddField("teams:", teamString, true);
                //Event is live
                JArray latestResults = await GetLatestResultsOfEvent(ushort.Parse(eventStats.GetValue("id").ToString()));
                string latestResultsString = "";
                if (latestResults == null)
                {
                    latestResultsString = "n.A";
                }
                else
                {
                    foreach (JObject jObj in latestResults)
                    {
                        if (latestResults.IndexOf(jObj) > 4) { break; }
                        JObject team1 = JObject.Parse(jObj.GetValue("team1").ToString());
                        JObject team2 = JObject.Parse(jObj.GetValue("team2").ToString());
                        string matchLink = $"https://www.hltv.org/matches/{jObj.GetValue("id")}/{team1.GetValue("name").ToString().ToLower().Replace(" ", "-")}-vs-" +
                            $"{team2.GetValue("name").ToString().ToLower().Replace(" ", "-")}";
                        latestResultsString += $"[{team1.GetValue("name")} vs. {team2.GetValue("name")}]({matchLink})\n";
                    }
                }
                builder.AddField("latest results:", latestResultsString, true);
                builder.AddField("\u200b", "\u200b", true);
            }
            builder.WithColor(Color.Gold);
            builder.WithThumbnailUrl(eventStats.GetValue("logo").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventStats.GetValue("id")}/{eventStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventLink);
            builder.WithCurrentTimestamp();
            builder.WithFooter(Tools.GetRandomFooter(Context.Guild, Context.Client));
            await ReplyAsync(embed: builder.Build());
            */
        }
        #endregion
    }
}
