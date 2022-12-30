using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Modules;

public static class HltvEvents
{
    private const string CurrentEventsPath = "./cache/events/currentEvents.json";
    private const string OngoingEventsPath = "./cache/events/ongoingEvents.json";
    private const string PastEventsPath = "./cache/events/pastEvents.json";

    private static async Task<EventPreview[]> GetEvents()
    {
        GetEvents request = new();
        EventPreview[] events = await request.SendRequest<EventPreview[]>();
        Tools.SaveToFile(CurrentEventsPath, events);
        return events;
    }

    private static async Task<EventPreview[]> GetPastEvents()
    {
        string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddMonths(-1));
        string endDate = Tools.GetHltvTimeFormat(DateTime.Now);

        GetPastEvents request = new GetPastEvents{StartDate = startDate, EndDate = endDate};
        return await request.SendRequest<EventPreview[]>();
    }
    
    private static async Task<IEnumerable<EventPreview>> GetNewEvents(string path, Func<Task<EventPreview[]>> getEvents)
    {
        if (!await AutomatedMessageHelper.VerifyFile(path, getEvents))
        {
            return Array.Empty<EventPreview>();
        }

        EventPreview[] oldEvents = Tools.ParseFromFile<EventPreview[]>(path);
        EventPreview[] newEvents = await getEvents();
        Tools.SaveToFile(path, newEvents);

        return from newEvent in newEvents
            let found = oldEvents.Any(oldEvent => newEvent.Id == oldEvent.Id)
            where !found select newEvent;
    }

    private static async Task<IEnumerable<EventPreview>> GetNewOngoingEvents()
    {
        // Customized Tools.VerifyFile
        if (File.Exists(OngoingEventsPath))
        {
            try
            {
                JsonDocument.Parse(await File.ReadAllTextAsync(OngoingEventsPath));
            }
            catch (JsonException)
            {
                Tools.SaveToFile(OngoingEventsPath, (await GetEvents()).Where(obj => obj.DateStart < DateTimeOffset.Now.ToUnixTimeMilliseconds()));
                return Array.Empty<EventPreview>();
            }
        }
        else
        {
            Tools.SaveToFile(OngoingEventsPath, (await GetEvents()).Where(obj => obj.DateStart < DateTimeOffset.Now.ToUnixTimeMilliseconds()));
            return new List<EventPreview>();
        }

        EventPreview[] events = await GetEvents();
        EventPreview[] oldEvents = Tools.ParseFromFile<EventPreview[]>(OngoingEventsPath);
        IEnumerable<EventPreview> newEvents = events.Where(obj => obj.DateStart < DateTimeOffset.Now.ToUnixTimeMilliseconds());
        Tools.SaveToFile(OngoingEventsPath, newEvents);
        
        return from newEvent in newEvents
            let found = oldEvents.Any(oldEvent => oldEvent.Id == newEvent.Id)
            where !found select newEvent;
    }

    public static async Task SendNewStartedEvents()
    {
        Stopwatch watch = new(); watch.Start();
        foreach (GetEvent request in from startedEvent in await GetNewOngoingEvents() select new GetEvent{Id = startedEvent.Id})
        {
            await Tools.SendMessagesWithWebhook(x => x.EventWebhookId != null,
                x => x.EventWebhookId, x=> x.EventWebhookToken , (await request.SendRequest<FullEvent>()).ToStartedEmbed());
        }
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched started events ({watch.ElapsedMilliseconds}ms)");
    }

    public static async Task SendNewPastEvents()
    {
        Stopwatch watch = new(); watch.Start();
        IEnumerable<EventPreview> events = await GetNewEvents(PastEventsPath, GetPastEvents);
        foreach (GetEvent request in from startedEvent in events select new GetEvent{Id = startedEvent.Id})
        {
            await Tools.SendMessagesWithWebhook(x => x.EventWebhookId != null,
                x => x.EventWebhookId, x=> x.EventWebhookToken , (await request.SendRequest<FullEvent>()).ToPastEmbed());
        }
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched past events ({watch.ElapsedMilliseconds}ms)");
    }

    public static async Task SendEvents(SocketSlashCommand arg)
    {
        EmbedBuilder builder = new();
        List<EventPreview> ongoingEvents = Tools.ParseFromFile<List<EventPreview>>(OngoingEventsPath);

        builder.WithTitle("ONGOING EVENTS")
            .WithColor(Color.Gold)
            .WithDescription("Please select an event for more information");

        SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select an event")
            .WithCustomId("ongoingEventsMenu")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (EventPreview ongoingEvent in ongoingEvents)
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

        ComponentBuilder compBuilder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder);

        await arg.ModifyOriginalResponseAsync(msg => { msg.Embed = builder.Build(); msg.Components = compBuilder.Build(); });
    }
    public static async Task SendUpcomingEvents(SocketSlashCommand arg)
    {
        List<EventPreview> upcomingEvents = Tools.ParseFromFile<List<EventPreview>>(CurrentEventsPath);
        
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
                    $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()} | {upcomingEvent.Location.Name}", new Emoji("⭐"));
            } 
            else
            {
                menuBuilder.AddOption(upcomingEvent.Name, upcomingEvent.Id.ToString(),
                    $"{startDate.ToShortDateString()} - {endDate.ToShortDateString()} | {upcomingEvent.Location.Name}");
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
        List<Result> results;
        try
        {
            GetEvent request = new GetEvent{Id = int.Parse(arg.Data.Values.First())};
            fullEvent = await request.SendRequest<FullEvent>();
            GetResults requestResults = new GetResults { EventIds = new[] { fullEvent.Id } };
            results = await requestResults.SendRequest<List<Result>>();
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
        SelectMenuBuilder builder = menu!.ToBuilder();

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
        await arg.ModifyOriginalResponseAsync(message =>
        {
            message.Embed = fullEvent.ToFullEmbed(results);
            message.Components = compBuilder.Build();
        });
    }
    public static async Task SendEvent(SocketSlashCommand arg)
    {
        Embed embed;
        GetEventByName request = new GetEventByName{Name = arg.Data.Options.First().Value.ToString()};
        try
        {
            FullEvent myEvent = await request.SendRequest<FullEvent>();
            GetResults requestResults = new GetResults { EventIds = new[] { myEvent.Id } };
            embed = myEvent.ToFullEmbed(await requestResults.SendRequest<List<Result>>());
        }
        catch (ApiError ex)
        {
            embed = ex.ToEmbed();
        }
        catch (DeploymentException ex)
        {
            embed = ex.ToEmbed();
        }
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }
}