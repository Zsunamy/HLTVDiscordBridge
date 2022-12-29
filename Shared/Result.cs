using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Discord;
using HLTVDiscordBridge.Modules;

namespace HLTVDiscordBridge.Shared;

public class Result
{
    public int Id { get; set; }
    public int Stars { get; set; }
    public ulong Date { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public ResultEvent Event { get; set; }
    
    [JsonPropertyName("result")]
    public ResultResult ResultResult { get; set; }
    public string Format { get; set; }

    [JsonIgnore]
    public string Link => $"https://www.hltv.org/matches/{Id}/" +
                          $"{Team1.Name.ToLower().Replace(" ", "-")}-vs-{Team2.Name.ToLower().Replace(" ", "-")}-" +
                          $"{Event.Name.ToLower().Replace(" ", "-")}";
    public (Embed, MessageComponent) ToEmbedAndComponent(Match match)
    {
        EmbedBuilder builder = new();
        string title = ResultResult.Team1 > ResultResult.Team2 ? $"👑 {match.Team1.Name} vs. {match.Team2.Name}" :
            $"{match.Team1.Name} vs. {match.Team2.Name} 👑";
        builder.WithTitle(title)
            .WithColor(Color.Red)
            .AddField("event:", $"[{match.Event.Name}]({match.Event.Link})\n{match.Significance}")
            .AddField("winner:", $"[{match.WinnerTeam.Name}]({match.WinnerTeam.Link})", true)
            .AddField("format:", $"{Tools.GetFormatFromAcronym(match.Format.Type)} ({match.Format.Location})", true)
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
            if(map.Result != null)
            {
                string mapHalfResultString = 
                    map.Result.HalfResults.Aggregate("", (current, mapHalfResult) => current + (current == "" ? $"{mapHalfResult.Team1Rounds}:{mapHalfResult.Team2Rounds}" : $" | {mapHalfResult.Team1Rounds}:{mapHalfResult.Team2Rounds}"));
                mapsString += $"{Tools.GetMapNameByAcronym(map.Name)} ({map.Result.Team1TotalRounds}:{map.Result.Team2TotalRounds}) ({mapHalfResultString})\n";
            }
            else
            {
                mapsString += $"~~{Tools.GetMapNameByAcronym(map.Name)}~~\n";
            }
        }
        builder.AddField("maps:", mapsString);
                
        if (match.Highlights.Length != 0)
        {
            IEnumerable<Highlight> highlights = match.Highlights.Take(2);
            //match.Highlights.CopyTo(0, highlights, 0, 2);
            string highlightsString = highlights.Aggregate
                ("", (current, highlight) => current + $"[{Tools.SpliceText(highlight.Title, 35)}]({highlight.Link})\n\n");
            builder.AddField("highlights:", highlightsString);
        }
        // Message Component
        ComponentBuilder compBuilder = new();
        compBuilder.WithButton("match statistics",
            match.Format.Type == "bo1" ? "overallstats_bo1" : "overallstats_def");
        return (builder.Build(), compBuilder.Build());
    }
}