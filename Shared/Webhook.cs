namespace HLTVDiscordBridge.Shared;

public readonly struct Webhook
{
    public Webhook(ulong? id, string token)
    {
        Id = id;
        Token = token;
    }

    public ulong? Id { get; }
    public string Token { get; }
}