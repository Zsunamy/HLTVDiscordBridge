using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Modules;

public class ServerConfig
{
    [BsonId] public ObjectId Id { get; set; } = new();
    public ulong GuildId { get; set; }
    public ulong NewsChannelID { get; set; }
    public Webhook News { get; set; }
    public Webhook Results { get; set; }
    public Webhook Events { get; set; }
    public int MinimumStars { get; set; }
    public bool OnlyFeaturedEvents { get; set; }
    public bool NewsOutput { get; set; }
    public bool ResultOutput { get; set; }
    public bool EventOutput { get; set; }

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
    
    public FilterDefinition<ServerConfig> GetFilter()
    {
        return Builders<ServerConfig>.Filter.Eq(x => x.GuildId, GuildId);
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