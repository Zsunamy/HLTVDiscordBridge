using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class Highlight
    {
        public Highlight(JObject jObject)
        {
            title = jObject.TryGetValue("title", out JToken titleTok) ? titleTok.ToString() : null;
            link = jObject.TryGetValue("link", out JToken linkTok) ? linkTok.ToString() : null;
        }

        public string title { get; set; }
        public string link { get; set; }
    }
}
