using System;
using System.Linq.Expressions;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Notifications;

public class NewsNotifier : AbstractNotifier
{
    public static readonly AbstractNotifier Instance = new NewsNotifier();

    private NewsNotifier() {}

    protected override Webhook GetWebhook(ServerConfig config)
    {
        return config.News;
    }

    protected override void SetWebhook(ServerConfig config, Webhook webhook)
    {
        config.News = webhook;
    }

    protected override void IncStats(int count)
    {
        StatsTracker.GetStats().NewsSent += count;
    }

    protected override Expression<Func<ServerConfig, bool>> GetFilter()
    {
        return config => config.News != null;
    }
}