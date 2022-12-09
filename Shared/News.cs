using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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

        public static List<News> ParseFromFile(string path)
        {
            return JsonSerializer.Deserialize<List<News>>(File.ReadAllText(path), ApiRequestBody.SerializeOptions);
        }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
    }
}
