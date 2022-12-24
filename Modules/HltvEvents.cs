using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Modules;

public static class HltvEvents
{
    private const string CurrentEventsPath = "./cache/events/currentEvents.json";
    private const string PastEventsPath = "./cache/events/pastEvents.json";

    private static async Task<List<EventPreview>> GetEvents()
    {
        ApiRequestBody request = new();
        return await request.SendRequest<List<EventPreview>>("GetEvents");
    }

    private static async Task<List<EventPreview>> GetPastEvents(string a)
    {
        string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddMonths(-1));
        string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
        GetPastEvents request = new(startDate, endDate);
        return await request.SendRequest<List<EventPreview>>("GetPastEvents");
    }

    private static async Task<List<EventPreview>> GetNewStartedEvents()
    {
        if (!await AutomatedMessageHelper.VerifyFile(CurrentEventsPath, GetEvents))
        {
            return new List<EventPreview>();
        }

        List<EventPreview> oldEvents = AutomatedMessageHelper.ParseFromFile<EventPreview>(CurrentEventsPath);
        List<EventPreview> newEvents = await GetEvents();
        AutomatedMessageHelper.SaveToFile(CurrentEventsPath, newEvents);
        return (from oldEvent in oldEvents 
            from newEvent in newEvents 
            where oldEvent.Id == newEvent.Id && newEvent.DateStart > DateTimeOffset.Now.ToUnixTimeSeconds()
            select newEvent).ToList();
    }

    public static async Task SendNewStartedEvents()
    {
        foreach (EventPreview startedEvent in await GetNewStartedEvents())
        {
            FullEvent fullEvent = await GetFullEvent(startedEvent);
            await Tools.SendMessagesWithWebhook(x => x.EventWebhookId != null,
                x => x.EventWebhookId, x=> x.EventWebhookToken , GetEventStartedEmbed(fullEvent));
        }
    }

    public static async Task AktEvents()
    {
        Stopwatch watch = new(); watch.Start();
        List<OngoingEventPreview> startedEvents = await GetStartedEvents();
        if (startedEvents.Count > 0)
        {
            foreach (OngoingEventPreview startedEvent in startedEvents)
            {
                FullEvent fullEvent = await GetFullEvent(startedEvent);
                if (fullEvent != null)
                {
                    await Tools.SendMessagesWithWebhook(x => x.EventWebhookId != null,
                        x => x.EventWebhookId, x=> x.EventWebhookToken , GetEventStartedEmbed(fullEvent));
                }
            }
            Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched events ({watch.ElapsedMilliseconds}ms)");
        }

        List<EventPreview> endedEvents = await GetEndedEvents();
        if (endedEvents.Count > 0)
        {
            foreach (EventPreview endedEvent in endedEvents)
            {
                FullEvent fullEvent = await GetFullEvent(endedEvent);
                if (fullEvent != null)
                {
                    await Tools.SendMessagesWithWebhook(x => x.EventWebhookId != null,
                        x => x.EventWebhookId, x=> x.EventWebhookToken , GetEventStartedEmbed(fullEvent), null);
                }
            }
        }
    }
    
    //3 Funktionen:
    //GetOngoingEvents => /getEvents => Neu in ongoing = Event started
    //GetUpcomingEvents => /getEvents
    //GetPastEvents => /getpastevents => Neu in Past = Event ended
    //
    //2 Funktionen:
    //GetStartedEvents
    //GetEndedEvents

    public static async Task<List<OngoingEventPreview>> GetOngoingEvents()
    {
        List<OngoingEventPreview> ongoingEvents = new();
        Directory.CreateDirectory("./cache/events");

        var req = await Tools.RequestApiJArray("getEvents", new List<string>(), new List<string>());

        List<EventPreview> upcomingAndOngoingEvents = new();
        foreach(JToken eventTok in req)
        {                
            upcomingAndOngoingEvents.Add(new EventPreview(eventTok as JObject));
        }
            

        foreach(EventPreview eventPreview in upcomingAndOngoingEvents)
        {
            if(UnixTimeStampToDateTime(eventPreview.DateEnd) > DateTime.UtcNow && UnixTimeStampToDateTime(eventPreview.DateStart) < DateTime.UtcNow)
            {
                ongoingEvents.Add(new OngoingEventPreview(JObject.FromObject(eventPreview)));
            }
        }

        File.WriteAllText("./cache/events/ongoing.json", JArray.FromObject(ongoingEvents).ToString());

        return ongoingEvents;
    }
    public static async Task<List<EventPreview>> GetUpcomingEvents()
    {
        ApiRequestBody request = new();
        List<EventPreview> events = await request.SendRequest<List<EventPreview>>("getEvents");
        List<EventPreview> upcomingEvents = new();
        Directory.CreateDirectory("./cache/events");

        var req = await Tools.RequestApiJArray("getEvents", new List<string>(), new List<string>());

        List<EventPreview> upcomingAndOngoingEvents = new();
        foreach (JToken eventTok in req)
        {
            upcomingAndOngoingEvents.Add(new EventPreview(eventTok as JObject));
        }

        foreach (EventPreview eventPreview in upcomingAndOngoingEvents)
        {
            if (eventPreview.Location != null)
            {
                upcomingEvents.Add(eventPreview);
            }
        }

        File.WriteAllText("./cache/events/upcoming.json", JArray.FromObject(upcomingEvents).ToString());

        return upcomingEvents;
    }
    public static async Task<List<EventPreview>> GetPastEvents()
    {
        List<EventPreview> pastEvents = new();
        Directory.CreateDirectory("./cache/events");

        List<string> properties = new();
        List<string> values = new();
        properties.Add("startDate");
        properties.Add("endDate");

        DateTime date = DateTime.Now;
        string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddMonths(-1));
        string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
        values.Add(startDate);
        values.Add(startDate);

        var req = await Tools.RequestApiJArray("getPastEvents", properties, values);


        foreach (JToken eventTok in req)
        {
            pastEvents.Add(new EventPreview(eventTok as JObject));
        }

        File.WriteAllText("./cache/events/past.json", JArray.FromObject(pastEvents).ToString());

        return pastEvents;
    }
    public static async Task<List<OngoingEventPreview>> GetStartedEvents()
    {
        Directory.CreateDirectory("./cache/events");
        if(!File.Exists("./cache/events/ongoing.json")) { await GetOngoingEvents(); return null; }

        List<OngoingEventPreview> oldOngoingEvents = new();
        foreach(JToken oldOngoingEvent in JArray.Parse(File.ReadAllText("./cache/events/ongoing.json")))
        {
            oldOngoingEvents.Add(new OngoingEventPreview(oldOngoingEvent as JObject));
        }

        List<OngoingEventPreview> newOngoingEvents = await GetOngoingEvents();

        List<OngoingEventPreview> startedEvents = new();
        foreach (OngoingEventPreview newOngoingEvent in newOngoingEvents)
        {
            bool started = true;
            foreach(OngoingEventPreview oldOngoingEvent in oldOngoingEvents)
            {
                if(newOngoingEvent.Id == oldOngoingEvent.Id)
                {
                    started = false;
                }
            }
            if(started)
            {
                startedEvents.Add(newOngoingEvent);
            }
        }

        File.WriteAllText("./cache/events/newongoing.json", JArray.FromObject(startedEvents).ToString());
        return startedEvents;
    }
    private static async Task<List<EventPreview>> GetEndedEvents()
    {
        
        Directory.CreateDirectory("./cache/events");
        if (!File.Exists("./cache/events/past.json")) { await GetPastEvents(); return null; }

        List<EventPreview> oldPastEvents = new();
        foreach (JToken oldPastEvent in JArray.Parse(File.ReadAllText("./cache/events/past.json")))
        {
            oldPastEvents.Add(new EventPreview(oldPastEvent as JObject));
        }

        List<EventPreview> newPastEvents = await GetPastEvents();

        List<EventPreview> endedEvents = new();
        foreach (EventPreview newPastEvent in newPastEvents)
        {
            bool started = true;
            foreach (EventPreview oldPastEvent in oldPastEvents)
            {
                if (newPastEvent.Id == oldPastEvent.Id)
                {
                    started = false;
                    break;
                }
            }
            if (started)
            {
                endedEvents.Add(newPastEvent);
            }
        }

        File.WriteAllText("./cache/events/newendedevents.json", JArray.FromObject(endedEvents).ToString());
        return endedEvents;
    }
    static async Task<FullEvent> GetFullEvent(OngoingEventPreview eventPreview)
    {
        return await GetFullEvent(eventPreview.Id);
    }
    static async Task<FullEvent> GetFullEvent(EventPreview eventPreview)
    {
        return await GetFullEvent(eventPreview.Id);
    }
    public static async Task<FullEvent> GetFullEvent(int eventId)
    {
        List<string> properties = new();
        List<string> values = new();
        properties.Add("id");
        values.Add(eventId.ToString());
        try { var req = await Tools.RequestApiJObject("getEvent", properties, values); return new FullEvent(req); }
        catch (HltvApiExceptionLegacy) { throw; }
    }
    public static async Task<FullEvent> GetFullEvent(string eventName)
    {
        List<string> properties = new();
        List<string> values = new();
        properties.Add("name");
        values.Add(eventName);
        try
        {
            JObject req = await Tools.RequestApiJObject("getEventByName", properties, values);
            return new FullEvent(req);
        }
        catch (HltvApiExceptionLegacy) { throw; }
    }
    public static Embed GetEventEndedEmbed(FullEvent eventObj)
    {
        EmbedBuilder builder = new();
        if (eventObj == null) { return null; }
        builder.WithTitle($"{eventObj.Name} just ended!");
        builder.AddField("startDate:", UnixTimeStampToDateTime(eventObj.DateStart).ToShortDateString(), true);
        builder.AddField("endDate:", UnixTimeStampToDateTime(eventObj.DateEnd).ToShortDateString(), true);
        builder.AddField("\u200b", "\u200b", true);
        builder.AddField("prize pool:", eventObj.PrizePool, true);
        builder.AddField("location:", eventObj.Location.name, true);
        builder.AddField("\u200b", "\u200b", true);

        List<string> prizeList = new();
        foreach (Prize prize in eventObj.PrizeDistribution)
        {
            if(string.Join("\n", prizeList).Length > 600)
            {
                prizeList.Add($"and {eventObj.PrizeDistribution.Count - eventObj.PrizeDistribution.IndexOf(prize)} more");
                break;
            }
            List<string> prizes = new();
            if(prize.prize != null)
                prizes.Add($"wins: {prize.prize}"); 
            if(prize.qualifiesFor != null)
                prizes.Add($"qualifies for: [{prize.qualifiesFor.name}]({prize.qualifiesFor.link})"); 
            if(prize.otherPrize != null)
                prizes.Add($"qualifies for: {prize.otherPrize}");

            prizeList.Add($"{prize.place} [{prize.team.name}]({prize.team.link}) {string.Join(" & ", prizes)}");
        }
        if(prizeList.Count > 0)
        {
            builder.AddField("results:", string.Join("\n", prizeList));
        }
            
        builder.WithColor(Color.Gold);
        builder.WithThumbnailUrl(eventObj.Logo);
        builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventObj.Link);
        builder.WithCurrentTimestamp();
        return builder.Build();
    }
    public static Embed GetEventStartedEmbed(FullEvent eventObj)
    {
        EmbedBuilder builder = new();
        if (eventObj == null) { return null; }
        builder.WithTitle($"{eventObj.Name} just started!");
        builder.AddField("startDate:", UnixTimeStampToDateTime(eventObj.DateStart).ToShortDateString(), true);
        builder.AddField("endDate:", UnixTimeStampToDateTime(eventObj.DateEnd).ToShortDateString(), true);
        builder.AddField("\u200b", "\u200b", true);
        builder.AddField("prize pool:", eventObj.PrizePool, true);
        builder.AddField("location:", eventObj.Location.name, true);
        builder.AddField("\u200b", "\u200b", true);
        List<string> teams = new();
        foreach (EventTeam team in eventObj.Teams)
        {
            if (string.Join("\n", teams).Length > 600)
            {
                teams.Add($"and {eventObj.Teams.Count - eventObj.Teams.IndexOf(team)} more");
                break;
            }
            teams.Add($"[{team.name}]({team.link})");
        }
        if(teams.Count > 0)
            builder.AddField("teams:", string.Join("\n", teams));
        builder.WithColor(Color.Gold);
        builder.WithThumbnailUrl(eventObj.Logo);
        builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventObj.Link);
        builder.WithCurrentTimestamp();
        return builder.Build();
    }
    public static async Task<Embed> GetEventEmbed(FullEvent eventObj)
    {
        EmbedBuilder builder = new();
        builder.WithTitle($"{eventObj.Name}")
            .WithColor(Color.Gold)
            .WithThumbnailUrl(eventObj.Logo)
            .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", eventObj.Link)
            .WithFooter(Tools.GetRandomFooter())
            .WithCurrentTimestamp();
        DateTime startDate = UnixTimeStampToDateTime(eventObj.DateStart);
        DateTime endDate = UnixTimeStampToDateTime(eventObj.DateEnd);
        string start = startDate > DateTime.UtcNow ? "starting" : "started";
        string end = endDate > DateTime.UtcNow ? "ending" : "ended";
        builder.AddField(start, startDate.ToShortDateString(), true)
            .AddField(end, endDate.ToShortDateString(), true)
            .AddField("\u200b", "\u200b", true)
            .AddField("prize pool:", eventObj.PrizePool, true)
            .AddField("location:", eventObj.Location.name, true)
            .AddField("\u200b", "\u200b", true);

        List<string> teams = new();
        foreach (EventTeam team in eventObj.Teams)
        {
            if (string.Join("\n", teams).Length > 600)
            {
                teams.Add($"and {eventObj.Teams.Count - eventObj.Teams.IndexOf(team)} more");
                break;
            }
            teams.Add($"[{team.name}]({team.link})");
        }
        if (teams.Count > 0)
            builder.AddField("teams:", string.Join("\n", teams));

        if (startDate > DateTime.UtcNow && endDate > DateTime.UtcNow)
        {
            //upcoming                
        } 
        else if(startDate < DateTime.UtcNow && endDate > DateTime.UtcNow)
        {
            List<Shared.Result> results = await HltvResults.GetMatchResultsOfEvent(eventObj.Id);
            List<string> matchResultString = new();
            if(results.Count > 0)
            {
                    
                foreach (Shared.Result result in results)
                {
                    if (string.Join("\n", matchResultString).Length > 700)
                    {
                        matchResultString.Add($"and {results.Count - results.IndexOf(result)} more");
                        break;
                    }
                    matchResultString.Add($"[{result.Team1.name} vs. {result.Team2.name}]({result.Link})");
                }
                builder.AddField("latest results:", string.Join("\n", matchResultString), true);
            }                
            //live
        } 
        else
        {
            List<string> prizeList = new();
            foreach (Prize prize in eventObj.PrizeDistribution)
            {
                if (string.Join("\n", prizeList).Length > 600)
                {
                    prizeList.Add($"and {eventObj.PrizeDistribution.Count - eventObj.PrizeDistribution.IndexOf(prize)} more");
                    break;
                }
                List<string> prizes = new();
                if (prize != null)
                    prizes.Add($"wins: {prize.prize}");
                if (prize.qualifiesFor != null)
                    prizes.Add($"qualifies for: [{prize.qualifiesFor.name}]({prize.qualifiesFor.link})");
                if (prize.otherPrize != null)
                    prizes.Add($"qualifies for: {prize.otherPrize}");

                prizeList.Add($"{prize.place} [{prize.team.name}]({prize.team.link}) {string.Join(" & ", prizes)}");
            }
            if (prizeList.Count > 0)
            {
                builder.AddField("results:", string.Join("\n", prizeList));
            }
            //past
        }

        return builder.Build();
    }
    public static async Task SendEvents(SocketSlashCommand arg)
    {
        await arg.DeferAsync();

        EmbedBuilder builder = new();

        List<OngoingEventPreview> ongoingEvents = new();
        if (!File.Exists("./cache/events/ongoing.json") || File.GetCreationTimeUtc("./cache/events/ongoing.json") < DateTime.UtcNow.AddMinutes(-10))
        {
            ongoingEvents = await GetOngoingEvents();
        }
        else
        {
            foreach (JToken ongoingEvent in JArray.Parse(File.ReadAllText("./cache/events/ongoing.json")))
            {
                ongoingEvents.Add(new OngoingEventPreview(ongoingEvent as JObject));
            }
        }

        builder.WithTitle("ONGOING EVENTS")
            .WithColor(Color.Gold)
            .WithDescription("Please select an event for more information");

        var menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select an event")
            .WithCustomId("ongoingEventsMenu")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (OngoingEventPreview ongoingEvent in ongoingEvents)
        {
            DateTime startDate = UnixTimeStampToDateTime(ongoingEvent.DateStart);
            DateTime endDate = UnixTimeStampToDateTime(ongoingEvent.DateEnd);
            if (ongoingEvent.Featured)
            {
                menuBuilder.AddOption(ongoingEvent.Name, ongoingEvent.Id.ToString(), $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()}", new Emoji("⭐"));
            }
            else
            {
                menuBuilder.AddOption(ongoingEvent.Name, ongoingEvent.Id.ToString(), $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()}");
            }
        }

        var compBuilder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder);

        await arg.ModifyOriginalResponseAsync(msg => { msg.Embed = builder.Build(); msg.Components = compBuilder.Build(); });
    }
    public static async Task SendUpcomingEvents(SocketSlashCommand arg)
    {
        await arg.DeferAsync();

        EmbedBuilder builder = new();

        List<EventPreview> upcomingEvents = new();
        if(!File.Exists("./cache/events/upcoming.json") || File.GetCreationTimeUtc("./cache/events/upcoming.json") < DateTime.UtcNow.AddMinutes(-10))
        {
            upcomingEvents = await GetUpcomingEvents();
        } 
        else
        {
            foreach(JToken upcomingEvent in JArray.Parse(File.ReadAllText("./cache/events/upcoming.json")))
            {
                upcomingEvents.Add(new EventPreview(upcomingEvent as JObject));
            }
        }

        builder.WithTitle("UPCOMING EVENTS")
            .WithColor(Color.Gold)
            .WithDescription("Please select an event for more information");

        var menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select an event")
            .WithCustomId("upcomingEventsMenu")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach(EventPreview upcomingEvent in upcomingEvents)
        {
            DateTime startDate = UnixTimeStampToDateTime(upcomingEvent.DateStart);
            DateTime endDate = UnixTimeStampToDateTime(upcomingEvent.DateEnd);
            if(upcomingEvent.Featured)
            {
                menuBuilder.AddOption(upcomingEvent.Name, upcomingEvent.Id.ToString(), $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()} | {upcomingEvent.Location.name}", new Emoji("⭐"));
            } 
            else
            {
                menuBuilder.AddOption(upcomingEvent.Name, upcomingEvent.Id.ToString(), $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()} | {upcomingEvent.Location.name}");
            }
            if(menuBuilder.Options.Count > 24)
            {
                break;
            }
        }

        var compBuilder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder);

        await arg.ModifyOriginalResponseAsync(msg => { msg.Embed = builder.Build(); msg.Components = compBuilder.Build(); });
    }
    public static async Task SendEvent(SocketMessageComponent arg)
    {
        await arg.DeferAsync();
        Embed embed;
        try
        {
            FullEvent fullEvent = await GetFullEvent(int.Parse(arg.Data.Values.First()));
            embed = await GetEventEmbed(fullEvent);
            var msg = arg.Message;

            SelectMenuComponent menu = msg.Components.First().Components.First() as SelectMenuComponent;
            SelectMenuBuilder builder = menu.ToBuilder();

            foreach (SelectMenuOptionBuilder option in builder.Options)
            {
                if (option.IsDefault == true) { option.IsDefault = false; break; }
            }
            foreach (SelectMenuOptionBuilder option in builder.Options)
            {
                if (option.Value == arg.Data.Values.First())
                {
                    option.IsDefault = true; break;
                }
            }
            var compBuilder = new ComponentBuilder()
                .WithSelectMenu(builder);
            await arg.ModifyOriginalResponseAsync(msg => { msg.Embed = embed; msg.Components = compBuilder.Build(); });
        }
        catch (HltvApiExceptionLegacy e) { embed = ErrorHandling.GetErrorEmbed(e); await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed); }  
    }
    public static async Task SendEvent(SocketSlashCommand arg)
    {
        await arg.DeferAsync();
        Embed embed; 
        try { embed = await GetEventEmbed(await GetFullEvent(arg.Data.Options.First().Value.ToString())); }
        catch (HltvApiExceptionLegacy e) { embed = ErrorHandling.GetErrorEmbed(e); }
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddMilliseconds(double.Parse(unixTimeStamp.ToString())).ToUniversalTime();
        dtDateTime = dtDateTime.AddHours(1);
        return dtDateTime;
    }
}