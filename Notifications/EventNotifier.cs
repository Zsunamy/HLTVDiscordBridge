using System;
using System.Linq.Expressions;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Notifications;

public class EventNotifier : AbstractNotifier
{
    public static readonly AbstractNotifier Instance = new EventNotifier();

    private EventNotifier() {}
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

    protected override Expression<Func<ServerConfig, bool>> GetFilter()
    {
        return config => config.Events != null;
    }
}