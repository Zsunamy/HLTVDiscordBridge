using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HLTVDiscordBridge.Modules;

public class ServerConfig
{
    [BsonId] public ObjectId Id { get; set; } = new();
    public ulong GuildId { get; init; }
    public Webhook News { get; set; }
    public Webhook Results { get; set; }
    public Webhook Events { get; set; }
    public int MinimumStars { get; set; }
    public bool OnlyFeaturedEvents { get; set; }
    
    public IEnumerable<Webhook> GetWebhooks()
    {
        return new[] { News, Results, Events }.Where(webhook => webhook != null);
    }

    public async Task<Webhook> CheckIfConfigUsesWebhookOfChannel(ITextChannel channel)
    {
        return (from webhook in await channel.GetWebhooksAsync()
            select new Webhook { Id = webhook.Id, Token = webhook.Token }).FirstOrDefault(channelWebhook => 
            GetWebhooks().Aggregate(false, (b, currentWebhook) => 
                (currentWebhook.Id == channelWebhook.Id && currentWebhook.Token == channelWebhook.Token) || b));
    }

    public override bool Equals(object obj)
    {
        return obj is ServerConfig config && GuildId == config.GuildId;
    }

    public override int GetHashCode()
    {
        return GuildId.GetHashCode();
    }
}