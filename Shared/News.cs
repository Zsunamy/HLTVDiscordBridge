using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HLTVDiscordBridge.Shared
{
    public class News
    {
        public News(XmlNodeList nodeList)
        {
            title = nodeList[0].InnerText;
            description = nodeList[1].InnerText;
            link = nodeList[2].InnerText;
            id = ushort.Parse(link.Substring(26, 5));
        }
        public News(JObject jObject)
        {
            title = jObject.TryGetValue("title", out JToken titleTok) ? titleTok.ToString() : null;
            description = jObject.TryGetValue("description", out JToken descriptionTok) ? descriptionTok.ToString() : null;
            if(jObject.TryGetValue("link", out JToken linkTok))
            {
                if(linkTok.ToString().Contains("http"))
                {
                    link = linkTok.ToString();
                }
                else
                {
                    link = $"https://www.hltv.org{linkTok}";
                }
            } else { link = null; }
            id = link != null ? ushort.Parse(link.Substring(26, 5)) : (ushort)0;
        }

        public string title { get; set; }
        public string description { get; set; }
        public string link { get; set; }
        public ushort id { get; set; }
    }
}
