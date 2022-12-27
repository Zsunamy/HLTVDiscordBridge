﻿using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Shared;

public class Result
{
    public int Id { get; set; }
    public int Stars { get; set; }
    public ulong Date { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    [JsonPropertyName("Result")]
    public ResultResult ResultResult { get; set; }
    public string Format { get; set; }
    public string Link { get; set; }
    public async Task<(Embed, MessageComponent)> ToEmbedAndComponent()
    {
        GetMatch request = new(Id);
        Match match = await request.SendRequest<Match>();
        EmbedBuilder builder = new();
        string title = match.WinnerTeam.Name == match.Team1.Name ? $"👑 {match.Team1.Name} vs. {match.Team2.Name}" :
            $"{match.Team1.Name} vs. {match.Team2.Name} 👑";
        builder.WithTitle(title)
            .WithColor(Color.Red)
            .AddField("event:", $"[{match.EventObj.Name}]({match.EventObj.Link})\n{match.Significance}")
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
            if(map.MapResult != null)
            {
                string mapHalfResultString = 
                    map.MapResult.MapHalfResults.Aggregate("", (current, mapHalfResult) => current + (current == "" ? $"{mapHalfResult.Team1Rounds}:{mapHalfResult.Team2Rounds}" : $" | {mapHalfResult.Team1Rounds}:{mapHalfResult.Team2Rounds}"));
                mapsString += $"{Tools.GetMapNameByAcronym(map.Name)} ({map.MapResult.Team1TotalRounds}:{map.MapResult.Team2TotalRounds}) ({mapHalfResultString})\n";
            }
            else
            {
                mapsString += $"~~{Tools.GetMapNameByAcronym(map.Name)}~~\n";
            }
        }
        builder.AddField("maps:", mapsString);
                
        if (match.Highlights.Count != 0)
        {
            Highlight[] highlights = new Highlight[2];
            match.Highlights.CopyTo(0, highlights, 0, 2);
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