using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Notifications;

public class ResultNotifier : AbstractNotifier
{
    
    public static readonly AbstractNotifier Instance = new ResultNotifier();

    private ResultNotifier()
    {
        foreach (ServerConfig config in Config.GetCollection().Find(x => x.Results != null).ToEnumerable())
        {
            Subscribers.Add(config.GuildId, config);
        }
    }
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
}