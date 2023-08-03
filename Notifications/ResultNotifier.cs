using System;
using System.Linq.Expressions;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Notifications;

public class ResultNotifier : AbstractNotifier
{
    
    public static readonly AbstractNotifier Instance = new ResultNotifier();

    private ResultNotifier() {}
    protected override Webhook GetWebhook(ServerConfig config)
    {
        return config.Results;
    }

    protected override void SetWebhook(ServerConfig config, Webhook webhook)
    {
        config.Results = webhook;
    }

    protected override void IncStats(int count)
    {
        StatsTracker.GetStats().ResultsSent += count;
    }

    protected override Expression<Func<ServerConfig, bool>> GetFilter()
    {
        return config => config.Results != null;
    }
}