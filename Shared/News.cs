using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Shared
{
    public class News
    {
        [JsonIgnore]
        public const string Path = "./cache/news/news.json";
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
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

        public static List<News> ParseFromFile()
        {
            return JsonSerializer.Deserialize<List<News>>(File.ReadAllText(Path), ApiRequestBody.SerializeOptions);
        }

        public static void SaveToFile(List<News> news)
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(news, ApiRequestBody.SerializeOptions));
        }

        public Embed ToEmbed()
        {
            EmbedBuilder builder = new();

            string title = Title ?? "n.A";
            string description = Description ?? "n.A";
            string link = Link ?? "";

            builder.WithTitle(title).WithColor(Color.Blue);
            builder.AddField("description:", description);
            builder.WithAuthor("full story on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", link);
            builder.WithCurrentTimestamp();
            return builder.Build();
        }
    }
}
