using Discord.Commands;
using System.IO;
using System.Xml.Serialization;

namespace HLTVDiscordBridge
{
    public class ConfigClass
    {
        public string BotToken { get; set; }
        public int CheckResultsTimeInterval { get; set; }
        public int MinimumStars { get; set; }
    }

    public class Config : ModuleBase<SocketCommandContext>
    {
        public string BotToken;
        public int ResultsTimeInterval;
        public int minStars;

        XmlSerializer _xml;
        ConfigClass conf = new ConfigClass();
        public void LoadConfig()
        {
            _xml = new XmlSerializer(typeof(ConfigClass));
            FileStream stream = new FileStream("./config.xml", FileMode.Open);
            conf = (ConfigClass)_xml.Deserialize(stream);
            BotToken = conf.BotToken;
            ResultsTimeInterval = conf.CheckResultsTimeInterval;
            minStars = conf.MinimumStars;
            stream.Close();
        }
    }

}
