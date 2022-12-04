using System.IO;
using System.Xml.Serialization;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public class BotConfigHandler
{
    private static BotConfigHandler _instance;
    private readonly BotConfig _config;

    private BotConfigHandler()
    {
        XmlSerializer xml = new(typeof(BotConfig));
        FileStream stream = new("./config.xml", FileMode.Open);
        _config = (BotConfig)xml.Deserialize(stream);
        stream.Close();
    }

    public static BotConfig GetBotConfig()
    {
        _instance ??= new BotConfigHandler();
        return _instance._config;
    }
}