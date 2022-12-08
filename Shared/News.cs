using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Shared
{
    public class News
    {
        public News(JObject jObject)
        {
            Title = jObject.TryGetValue("title", out JToken titleTok) ? titleTok.ToString() : null;
            Description = jObject.TryGetValue("description", out JToken descriptionTok) ? descriptionTok.ToString() : null;
            if(jObject.TryGetValue("link", out JToken linkTok))
            {
                Link = linkTok.ToString().Contains("http") ? linkTok.ToString() : $"https://www.hltv.org{linkTok}";
            } else { Link = null; }
        }

        public News() {}

        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
    }
}
