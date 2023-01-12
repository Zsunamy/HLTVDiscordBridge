using System.Text.Json.Serialization;
using Discord;

namespace HLTVDiscordBridge.Shared;

public class RssNews
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }
    public long Date { get; set; }
    [JsonIgnore] public int Id => int.Parse(Link.Split('/')[^2]);
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