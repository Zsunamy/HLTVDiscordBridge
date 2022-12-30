using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Net;
using Discord.Webhook;

namespace HLTVDiscordBridge.Shared;

public class Webhook
{
    public ulong? Id { get; set; }
    public string Token { get; set; }
    
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
            catch (HttpException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}