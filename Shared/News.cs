using Discord;

namespace HLTVDiscordBridge.Shared;

public class News
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }
    public int Id { get; set; }
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