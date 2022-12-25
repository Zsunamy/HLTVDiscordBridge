using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Shared
{
    public class Result
    {
        public async Task<(Embed, MessageComponent)> ToEmbedAndComponent()
    {
        GetMatch request = new(Id);
        Match match = await request.SendRequest<Match>();
        EmbedBuilder builder = new();
        string title = match.WinnerTeam.name == match.Team1.name ? $"👑 {match.Team1.name} vs. {match.Team2.name}" :
            $"{match.Team1.name} vs. {match.Team2.name} 👑";
        builder.WithTitle(title)
            .WithColor(Color.Red)
            .AddField("event:", $"[{match.EventObj.name}]({match.EventObj.link})\n{match.Significance}")
            .AddField("winner:", $"[{match.WinnerTeam.name}]({match.WinnerTeam.link})", true)
            .AddField("format:", $"{Tools.GetFormatFromAcronym(match.Format.type)} ({match.Format.location})", true)
            .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", match.Link)
            .WithCurrentTimestamp();
        string footerString = "";
        Emoji emo = new("⭐");
        for (int i = 1; i <= Stars; i++)
        {
            footerString += emo;
        }
        builder.WithFooter(footerString);

        string mapsString = "";
        foreach(Map map in match.Maps)
        {
            if(map.mapResult != null)
            {
                string mapHalfResultString = 
                    map.mapResult.mapHalfResults.Aggregate("", (current, mapHalfResult) => current + (current == "" ? $"{mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}" : $" | {mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}"));
                mapsString += $"{Tools.GetMapNameByAcronym(map.name)} ({map.mapResult.team1TotalRounds}:{map.mapResult.team2TotalRounds}) ({mapHalfResultString})\n";
            }
            else
            {
                mapsString += $"~~{Tools.GetMapNameByAcronym(map.name)}~~\n";
            }
        }
        builder.AddField("maps:", mapsString);
            
        if (match.Highlights.Count != 0)
        {
            Highlight[] highlights = new Highlight[2];
            match.Highlights.CopyTo(0, highlights, 0, 2);
            string highlightsString = highlights.Aggregate
                ("", (current, highlight) => current + $"[{Tools.SpliceText(highlight.title, 35)}]({highlight.link})\n\n");
            builder.AddField("highlights:", highlightsString);
        }
        // Message Component
        ComponentBuilder compBuilder = new();
        compBuilder.WithButton("match statistics",
            match.Format.type == "bo1" ? "overallstats_bo1" : "overallstats_def");
        return (builder.Build(), compBuilder.Build());
    }

        public int Id { get; set; }
        public int Stars { get; set; }
        public ulong Date { get; set; }
        public Team Team1 { get; set; }
        public Team Team2 { get; set; }
        public ResultResult ResultResult { get; set; }
        public string Format { get; set; }
        public string Link { get; set; }
        
    }
}
