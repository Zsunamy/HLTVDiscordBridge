using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;
using ZstdSharp.Unsafe;

namespace HLTVDiscordBridge.Notifications;

public class EventNotifier : AbstractNotifier
{
    public static readonly AbstractNotifier Instance = new EventNotifier();

    private EventNotifier()
    {
        foreach (ServerConfig config in Config.GetCollection().Find(x => x.Events != null).ToEnumerable())
        {
            Subscribers.Add(config.GuildId, config);
        }
    }
    protected override Webhook GetWebhook(ServerConfig config)
    {
        return config.Events;
    }

    protected override void SetWebhook(ServerConfig config, Webhook webhook)
    {
        config.Events = webhook;
    }

    protected override void IncStats(int count)
    {
        StatsTracker.GetStats().EventsSent += count;
    }
}