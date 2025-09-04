using System;
using System.Linq.Expressions;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Repository;
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

    protected override bool GetMessageFilter(ServerConfig config, object data)
    {
        if (data is not int currentStars)
        {
            throw new InvalidCastException("Filter for minStars must be a integer.");
        }
        return config.MinimumStars <= currentStars;
    }

    protected override void SetWebhook(ServerConfig config, Webhook webhook)
    {
        config.Results = webhook;
    }

    protected override void IncStats(int count)
    {
        StatsTracker.GetStats().ResultsSent += count;
    }

    protected override Expression<Func<ServerConfig, bool>> GetConfigFilter()
    {
        return config => config.Results != null;
    }
}