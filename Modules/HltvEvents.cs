using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

    private static async Task<List<EventPreview>> GetNewEvents(string path, Func<Task<List<EventPreview>>> getEvents)
    {
        if (!await AutomatedMessageHelper.VerifyFile(path, getEvents))
        {
            return new List<EventPreview>();
        }

        List<EventPreview> oldEvents = AutomatedMessageHelper.ParseFromFile<EventPreview>(path);
        List<EventPreview> newEvents = await getEvents();
        AutomatedMessageHelper.SaveToFile(path, newEvents);

        return (from newEvent in newEvents
            let found = oldEvents.Any(oldEvent => newEvent.Id == oldEvent.Id)
            where !found select newEvent).ToList();
    }

    public static async Task SendNewStartedEvents()
    {
        foreach (EventPreview startedEvent in await GetNewEvents(CurrentEventsPath, GetEvents))
        {
            await Tools.SendMessagesWithWebhook(x => x.EventWebhookId != null,
                x => x.EventWebhookId, x=> x.EventWebhookToken , (await GetFullEvent(startedEvent)).ToStartedEmbed());
        }
    }

    public static async Task SendNewPastEvents()
    {
        foreach (EventPreview startedEvent in await GetNewEvents(PastEventsPath, GetPastEvents))
        {
            await Tools.SendMessagesWithWebhook(x => x.EventWebhookId != null,
                x => x.EventWebhookId, x=> x.EventWebhookToken , (await GetFullEvent(startedEvent)).ToStartedEmbed());
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
            if(Tools.UnixTimeToDateTime(eventPreview.DateEnd) > DateTime.UtcNow && Tools.UnixTimeToDateTime(eventPreview.DateStart) < DateTime.UtcNow)
            {
                ongoingEvents.Add(new OngoingEventPreview(JObject.FromObject(eventPreview)));
            }
        }

        File.WriteAllText("./cache/events/ongoing.json", JArray.FromObject(ongoingEvents).ToString());

        return ongoingEvents;
    }
    private static async Task<List<EventPreview>> GetPastEvents()
    {
        string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddMonths(-1));
        string endDate = Tools.GetHltvTimeFormat(DateTime.Now);

        GetPastEvents request = new(startDate, endDate);
        List<EventPreview> pastEvents = await request.SendRequest<List<EventPreview>>("GetPastEvents");

        AutomatedMessageHelper.SaveToFile(PastEventsPath, pastEvents);

        return pastEvents;
    }
    /*
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
                        x => x.EventWebhookId, x=> x.EventWebhookToken , fullEvent.ToStartedEmbed());
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
                        x => x.EventWebhookId, x=> x.EventWebhookToken , fullEvent.ToStartedEmbed(), null);
                }
            }
        }
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
    */
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
        JObject req = await Tools.RequestApiJObject("getEventByName", properties, values);
        return new FullEvent(req);
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
            DateTime startDate = Tools.UnixTimeToDateTime(ongoingEvent.DateStart);
            DateTime endDate = Tools.UnixTimeToDateTime(ongoingEvent.DateEnd);
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
        List<EventPreview> upcomingEvents = AutomatedMessageHelper.ParseFromFile<EventPreview>(CurrentEventsPath);
        
        EmbedBuilder builder = new();
        builder.WithTitle("UPCOMING EVENTS")
            .WithColor(Color.Gold)
            .WithDescription("Please select an event for more information");

        SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select an event")
            .WithCustomId("upcomingEventsMenu")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach(EventPreview upcomingEvent in upcomingEvents)
        {
            DateTime startDate = Tools.UnixTimeToDateTime(upcomingEvent.DateStart);
            DateTime endDate = Tools.UnixTimeToDateTime(upcomingEvent.DateEnd);
            if(upcomingEvent.Featured)
            {
                menuBuilder.AddOption(upcomingEvent.Name, upcomingEvent.Id.ToString(),
                    $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()} | {upcomingEvent.Location.name}", new Emoji("⭐"));
            } 
            else
            {
                menuBuilder.AddOption(upcomingEvent.Name, upcomingEvent.Id.ToString(),
                    $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()} | {upcomingEvent.Location.name}");
            }
            if(menuBuilder.Options.Count > 24)
            {
                break;
            }
        }

        ComponentBuilder compBuilder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder);

        await arg.ModifyOriginalResponseAsync(msg => { msg.Embed = builder.Build(); msg.Components = compBuilder.Build(); });
    }
    public static async Task SendEvent(SocketMessageComponent arg)
    {
        await arg.DeferAsync();
        FullEvent fullEvent;
        try
        {
            GetEvent request = new(int.Parse(arg.Data.Values.First()));
            fullEvent = await request.SendRequest<FullEvent>("GetEvent");
        }
        catch (ApiError ex)
        {
            await arg.ModifyOriginalResponseAsync(msg => msg.Embed = ex.ToEmbed());
            return;
        }
        catch (DeploymentException ex)
        {
            await arg.ModifyOriginalResponseAsync(msg => msg.Embed = ex.ToEmbed());
            return;
        }
        SocketUserMessage msg = arg.Message;

        SelectMenuComponent menu = msg.Components.First().Components.First() as SelectMenuComponent;
        SelectMenuBuilder builder = menu.ToBuilder();

        foreach (SelectMenuOptionBuilder option in builder.Options.Where(option => option.IsDefault == true))
        {
            option.IsDefault = false;
            break;
        }
        foreach (SelectMenuOptionBuilder option in builder.Options.Where(option => option.Value == arg.Data.Values.First()))
        {
            option.IsDefault = true;
            break;
        }
        ComponentBuilder compBuilder = new ComponentBuilder()
            .WithSelectMenu(builder);
        await arg.ModifyOriginalResponseAsync(message => { message.Embed = fullEvent.ToFullEmbed().Result; message.Components = compBuilder.Build(); });
    }
    public static async Task SendEvent(SocketSlashCommand arg)
    {
        await arg.DeferAsync();
        Embed embed;
        try
        {
            embed = await (await GetFullEvent(arg.Data.Options.First().Value.ToString())).ToFullEmbed();
        }
        catch (HltvApiExceptionLegacy e)
        {
            embed = ErrorHandling.GetErrorEmbed(e);
        }
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }
}