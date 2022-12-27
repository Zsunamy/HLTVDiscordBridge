using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public int Rank { get; set; }
    public List<TeamPlayer> Players { get; set; }
    public List<uint> RankingDevelopment { get; set; }
    public List<News> News { get; set; }
    public string? LocalThumbnailPath { get; set; }
    public string Link { get; set; }

    public async Task<Embed> ToEmbed(FullTeamStats stats)
    {
        EmbedBuilder builder = new();

        builder.WithTitle(Name);

        //TeamLink
        builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link);

        //Thumbnail            
        builder.WithThumbnailUrl($"attachment://{Name.ToLower().Replace(' ', '-')}_logo.png");

        //rank + development
        string rankDevString;
        if (RankingDevelopment.Count < 2)
        {
            rankDevString = "n.A";
        }
        else
        {
            short development = (short)(short.Parse(RankingDevelopment[^1].ToString()) - short.Parse(RankingDevelopment[^2].ToString()));
            Emoji emote;
            string rank = "--";
            if (Rank != 0) { rank = Rank.ToString(); }

            switch (development)
            {
                case < 0:
                    emote = new Emoji("⬆️");
                    rankDevString = $"{rank} ({emote} {Math.Abs(development)})";
                    break;
                case 0:
                    rankDevString = $"{rank} (⏺️ 0)";
                    break;
                default:
                    emote = new Emoji("⬇️"); rankDevString = $"{rank} ({emote} {development})";
                    break;
            }
        }
        //stats

        builder.AddField("stats:", "Ranking:\nRounds played:\nMaps played:\nWins/Draws/Losses:\nKills/Deaths:", true);
        builder.AddField("\u200b", $"{rankDevString}\n{stats.Overview.RoundsPlayed}\n{stats.Overview.MapsPlayed}\n" + 
                                   $"{stats.Overview.Wins}/{stats.Overview.Draws}/{stats.Overview.Losses}\n" + 
                                   $"{stats.Overview.TotalKills}/{stats.Overview.TotalDeaths} (K/D: {stats.Overview.KdRatio})", true);
        builder.AddField("\u200b", "\u200b", true);

        //team-member
        string lineUpString = "";
        if (Players.Count == 0)
        {
            lineUpString = "n.A";
        }
        else
        {
            lineUpString = Players.Aggregate(lineUpString, (current, pl) => current + $"[{pl.Name}]({pl.Link}) ({pl.Type})\n");
        }
        builder.AddField("member:", lineUpString, true);
        //map-stats
        string mapsStatsString = "";
        if (stats.MapStats == null)
        {
            mapsStatsString = "n.A";
        }
        else
        {
            foreach ((string name, TeamMapStats map)  in stats.MapStats.GetMostPlayedMaps())
            {
                mapsStatsString += $"\n**{Tools.GetMapNameByAcronym(name)}** ({map.WinRate}% winrate):\n{map.Wins} wins, {map.Losses} losses\n\n";
            }
            /*
            for(int i = 0; i < 2; i++)
            {
                var prop = JObject.FromObject(stats.MapStats).Properties().ElementAt(i);
                TeamMapStats map = new(prop.Value as JObject);
                mapsStatsString += $"\n**{Tools.GetMapNameByAcronym(prop.Name)}** ({map.WinRate}% winrate):\n{map.Wins} wins, {map.Losses} losses\n\n";
            }
            */
        }
        builder.AddField("most played maps:", mapsStatsString, true);
        builder.AddField("\u200b", "\u200b", true);

        //recentResults
        List<Result> recentResults = await HltvResults.GetMatchResults(Id);
        string recentResultsString = "";
        if (recentResults.Count == 0)
        { recentResultsString = "n.A";
        }
        else
        { 
            foreach (Result matchResult in recentResults)
            {
                string opponentTeam = matchResult.Team1.Name == Name ? matchResult.Team2.Name : matchResult.Team1.Name;
                recentResultsString += $"[vs. {opponentTeam}]({matchResult.Link})\n";

                if (recentResults.IndexOf(matchResult) == 3)
                {
                    break;
                }
            }            
        }
        builder.AddField("recent results:", recentResultsString, true);
        builder.AddField("\u200b", "\u200b", true);
        builder.WithCurrentTimestamp();
        builder.WithFooter("The stats shown were collected during the last 3 months");
        return builder.Build();
    }
}