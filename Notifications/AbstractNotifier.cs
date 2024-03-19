using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Repository;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Notifications;

public abstract class AbstractNotifier
{
    private ServerConfig[] Subscribers => ServerConfigRepository.GetByFilter(GetConfigFilter());

    protected abstract Webhook GetWebhook(ServerConfig config);
    protected abstract bool GetMessageFilter(ServerConfig config, object data);
    protected abstract void SetWebhook(ServerConfig config, Webhook webhook);
    protected abstract void IncStats(int count);
    protected abstract Expression<Func<ServerConfig, bool>> GetConfigFilter();

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
        await ServerConfigRepository.Update(config);
    }

    public async Task Cancel(ServerConfig config)
    {
        Webhook webhook = GetWebhook(config);
        if (webhook != null && !webhook.CheckIfWebhookIsUsed(config))
            await webhook.Delete();
        
        SetWebhook(config, null);
        await ServerConfigRepository.Update(config);
    }

    public async Task NotifyAll(object filterData, Embed embed, MessageComponent component = null)
    {
        ServerConfig[] subBuffer = Subscribers;
        foreach (ServerConfig config in subBuffer.Where(x => GetMessageFilter(x, filterData)))
        {
            try
            {
                await GetWebhook(config).ToDiscordWebhookClient().SendMessageAsync(embeds: new[] { embed }, components: component);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException or InvalidCastException)
                {
                    await Cancel(config);
                }

                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                StatsTracker.GetStats().MessagesSent -= 1;
                IncStats(-1);
            }
        }
            
        StatsTracker.GetStats().MessagesSent += subBuffer.Length;
        IncStats(subBuffer.Length);
    }
}