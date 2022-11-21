namespace HLTVDiscordBridge.Shared;

public struct Webhook
{
    public Webhook(ulong? id, string token)
    {
        Id = id;
        Token = token;
    }

    public ulong? Id { get; init; }
    public string Token { get; init; }
}