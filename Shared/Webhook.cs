using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Webhook;
using HLTVDiscordBridge.Modules;

namespace HLTVDiscordBridge.Shared;

public class Webhook
{
    public ulong Id { get; init; }
    public string Token { get; init; }
    
    public bool CheckIfWebhookIsUsed(ServerConfig config)
    {
        return config.GetWebhooks().GroupBy(x => x.Id).Any(g => g.Count() > 1 && g.Key == Id);
    }
    
    public async Task Delete()
    {
        try
        {
            await ToDiscordWebhookClient().DeleteWebhookAsync();
        }
        catch (HttpException) {}
        catch (InvalidOperationException) {}
    }

    public DiscordWebhookClient ToDiscordWebhookClient()
    {
        return new DiscordWebhookClient(Id, Token);
    }

    public static async Task<Webhook> CreateWebhook(ITextChannel channel)
    {
        IWebhook webhook = await channel.CreateWebhookAsync("HLTV", new FileStream("icon.png", FileMode.Open));
        return new Webhook { Id = webhook.Id, Token = webhook.Token };
    }
}