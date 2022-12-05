using System;
using System.IO;
using System.Xml.Serialization;

namespace HLTVDiscordBridge.Shared;

[Serializable]
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
    [NonSerialized]
    private static BotConfig _instance;
    private BotConfig() {}

    public static BotConfig GetBotConfig()
    {
        if (_instance == null)
        {
            XmlSerializer xml = new(typeof(BotConfig));
            FileStream stream = new("./config.xml", FileMode.Open);
            _instance = (BotConfig)xml.Deserialize(stream);
            stream.Close();
        }
        return _instance;
    }
}