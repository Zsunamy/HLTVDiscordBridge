using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Notifications;

public abstract class AbstractNotifier
{
    protected Dictionary<ulong, ServerConfig> Subscribers { get; } = new();
    
    protected abstract Webhook GetWebhook(ServerConfig config);
    protected abstract void SetWebhook(ServerConfig config, Webhook webhook);
    protected abstract void IncStats(int count);

    public async Task Enroll(ServerConfig config, ITextChannel channel)
    {
        Webhook webhook = GetWebhook(config);
        Webhook newWebhook;
        Webhook multiWebhook = await config.CheckIfConfigUsesWebhookOfChannel(channel);
        if (webhook != null && !webhook.CheckIfWebhookIsUsed(config))
            await webhook.Delete();
            
        if (multiWebhook == null)
            newWebhook = await Webhook.CreateWebhook(channel);
        else
            newWebhook = multiWebhook;
        
        SetWebhook(config, newWebhook);
        UpdateDefinition<ServerConfig> update = Builders<ServerConfig>.Update.Set(x => GetWebhook(x), newWebhook);
        await Config.GetCollection().UpdateOneAsync(config.GetFilter(), update);
        
        Subscribers.Add(config.GuildId, config);
    }

    public async Task Cancel(ServerConfig config)
    {
        Webhook webhook = GetWebhook(config);
        if (webhook != null && !webhook.CheckIfWebhookIsUsed(config))
            await webhook.Delete();
        
        SetWebhook(config, null);
        UpdateDefinition<ServerConfig> update = Builders<ServerConfig>.Update.Set(x => GetWebhook(x), null);
        await Config.GetCollection().UpdateOneAsync(config.GetFilter(), update);
        
        Subscribers.Remove(config.GuildId);
    }

    public async Task NotifyAll(Embed embed, MessageComponent component = null)
    {
        ServerConfig[] subscriberList = Subscribers.Select(pair => pair.Value).ToArray();

            IEnumerable<Task> status = subscriberList.Select(config => Task.Run(async () =>
            {
                try
                {
                    await config.News.ToDiscordWebhookClient().SendMessageAsync(embeds: new[] { embed }, components: component);
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException or InvalidCastException)
                    {
                        await Cancel(config);
                    }

                    StatsTracker.GetStats().MessagesSent -= 1;
                    IncStats(-1);
                    throw;
                }
        }));
            
        StatsTracker.GetStats().MessagesSent += subscriberList.Length;
        IncStats(subscriberList.Length);

        await Task.WhenAll(status);
    }
}