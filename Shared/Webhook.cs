using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.Webhook;

namespace HLTVDiscordBridge.Shared;

public class Webhook
{
    public ulong? Id { get; init; }
    public string Token { get; init; }
    
    public bool CheckIfWebhookIsUsed(ServerConfig config)
    {
        return new[] { config.ResultWebhookId, config.NewsWebhookId, config.EventWebhookId }
            .GroupBy(x => x).Any(g => g.Count() > 1 && g.Key == Id);
    }
    
    public async Task Delete()
    {
        if (Id != null)
        {
            try
            {
                DiscordWebhookClient client = new((ulong)Id, Token);
                await client.DeleteWebhookAsync();
            }
            catch (HttpException) {}
            catch (InvalidOperationException) {}
        }
    }

    public DiscordWebhookClient ToDiscordWebhookClient()
    {
        if (Id != null)
        {
            return new DiscordWebhookClient((ulong)Id, Token);
        }

        throw new InvalidCastException("Invalid Webhook Id provided!.");
    }

    public static async Task<Webhook> CreateWebhook(ITextChannel channel)
    {
        IWebhook webhook = await channel.CreateWebhookAsync("HLTV", new FileStream("icon.png", FileMode.Open));
        return new Webhook { Id = webhook.Id, Token = webhook.Token };
    }
}