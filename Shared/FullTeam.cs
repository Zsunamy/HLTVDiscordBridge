﻿using System;
using System.Linq;
using System.Text.Json.Serialization;
using Discord;
using HLTVDiscordBridge.Modules;

namespace HLTVDiscordBridge.Shared;

public class FullTeam
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Logo { get; set; }
    public string Twitter { get; set; }
    public Country Country { get; set; }
    public int? Rank { get; set; }
    public TeamPlayer[] Players { get; set; }
    public int[] RankingDevelopment { get; set; }
    public Article[] News { get; set; }
    public string Link { get; set; }
    
    [JsonIgnore]
    public string FormattedName => Name.ToLower().Replace(" ", "-");

    public Embed ToEmbed(FullTeamStats stats, Result[] recentResults)
    {
        EmbedBuilder builder = new();

        builder.WithTitle(Name);

        //TeamLink
        builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link);

        //Thumbnail            
        builder.WithThumbnailUrl($"attachment://logo.png");

        //rank + development
        string rankDevString;
        if (RankingDevelopment == null || RankingDevelopment.Length < 2)
            rankDevString = "n.A";
        else
        {
            int development = RankingDevelopment[^1] - RankingDevelopment[^2];
            string rank = Rank != 0 && Rank != null ? Rank.ToString() : "--";
            rankDevString = development switch
            {
                < 0 => $"{rank} (⬆️ {Math.Abs(development)})",
                0 => $"{rank} (⏺️ 0)",
                _ => $"{rank} (⬇️ {development})"
            };
        }
        
        //stats
        builder.AddField("stats:", "Ranking:\nRounds played:\nMaps played:\nWins/Draws/Losses:\nKills/Deaths:", true);
        builder.AddField("\u200b", $"{rankDevString}\n{stats.Overview.RoundsPlayed}\n{stats.Overview.MapsPlayed}\n" +
            $"{stats.Overview.Wins}/{stats.Overview.Draws}/{stats.Overview.Losses}\n" +
            $"{stats.Overview.TotalKills}/{stats.Overview.TotalDeaths} (K/D: {stats.Overview.KdRatio})", true);
        builder.AddField("\u200b", "\u200b", true);

        //team-member
        string lineUpString = "";
        lineUpString = Players.Length == 0 ? "n.A" : Players.Aggregate(lineUpString, (current, pl) => current + $"[{pl.Name}]({pl.Link}) ({pl.Type})\n");
        builder.AddField("member:", lineUpString, true);

        //map-stats
        string mapsStatsString = "";
        if (!stats.MapStats.GetMostPlayedMaps().Any())
        {
            mapsStatsString = "n.A";
        }
        else
        {
            foreach ((string name, TeamMapStats map) in stats.MapStats.GetMostPlayedMaps().Take(2))
            {
                mapsStatsString += $"\n**{Tools.GetMapNameByAcronym(name)}** ({map.WinRate}% winrate):\n{map.Wins} wins, {map.Losses} losses\n\n";
            }
        }

        builder.AddField("most played maps:", mapsStatsString, true);
        builder.AddField("\u200b", "\u200b", true);

        //recent Results
        string recentResultsString = "";
        if (!recentResults.Any())
            recentResultsString = "n.A";
        else
        {
            foreach (Result matchResult in recentResults.Take(4))
            {
                string opponentTeam = matchResult.Team1.Name == Name ? matchResult.Team2.Name : matchResult.Team1.Name;
                recentResultsString += $"[vs. {opponentTeam}]({matchResult.Link})\n";;
            }            
        }

        builder.AddField("recent results:", recentResultsString, true);
        builder.AddField("\u200b", "\u200b", true);

        builder.WithCurrentTimestamp();
        builder.WithFooter("Some of the stats shown were collected during the last 3 months.");

        return builder.Build();
    }
}