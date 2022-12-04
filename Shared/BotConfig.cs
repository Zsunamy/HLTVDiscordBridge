namespace HLTVDiscordBridge.Shared;

public class BotConfig
{
    public string BotToken { get; set; }
    public ulong ProductionBotId { get; set; }
    public int CheckResultsTimeInterval { get; set; }
    public int DelayBetweenRequests { get; set; }
    public string TopGgApiKey { get; set; }
    public string BotsGgApiKey { get; set; }
    public string ApiLink { get; set; }
    public string DatabaseLink { get; set; }
    public string Database { get; set; }
}