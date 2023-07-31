using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Notifications;

public class NewsNotifier : AbstractNotifier
{
    public static readonly AbstractNotifier Instance = new NewsNotifier();

    private NewsNotifier()
    {
        foreach (ServerConfig config in Config.GetCollection().Find(x => x.News != null).ToEnumerable())
        {
            Subscribers.Add(config.GuildId, config);
        }
    }

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
}