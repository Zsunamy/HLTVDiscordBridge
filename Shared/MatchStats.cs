using System.Collections.Generic;
using System.Globalization;
using Discord;

namespace HLTVDiscordBridge.Shared;

public class MatchStats
{
    public int StatsId { get; set; }
    public int MatchId { get; set; }
    public int[] MapStatIds { get; set; }
    public long Date { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public Event Event { get; set; }
    public MatchStatsPlayerTeams PlayerStats { get; set; }
    public string Link { get; set; }
    
    public Embed ToEmbed()
        {
            EmbedBuilder builder = new();

            builder.WithTitle($"PLAYERSTATS ({Team1.Name} vs. {Team2.Name})")
                .WithColor(Color.Red);

            List<string> team1PlayerNames = [];
            List<string> team1Kad = [];
            List<string> team1Rating = [];
            foreach (MatchStatsPlayer playerStats in PlayerStats.Team1)
            {
                team1PlayerNames.Add($"[{playerStats.Player.Name}]({playerStats.Player.Link})");
                team1Kad.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team1Rating.Add(playerStats.Rating.ToString(CultureInfo.InvariantCulture));
            }
            builder.AddField($"players ({Team1.Name}):", string.Join("\n", team1PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team1Kad), true);
            builder.AddField($"rating ({PlayerStats.Team1[0].RatingVersion})", string.Join("\n", team1Rating), true);

            List<string> team2PlayerNames = [];
            List<string> team2Kad = [];
            List<string> team2Rating = [];
            foreach (MatchStatsPlayer playerStats in PlayerStats.Team2)
            {
                team2PlayerNames.Add($"[{playerStats.Player.Name}]({playerStats.Player.Link})");
                team2Kad.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team2Rating.Add(playerStats.Rating.ToString(CultureInfo.InvariantCulture));
            }
            builder.AddField($"players ({Team2.Name}):", string.Join("\n", team2PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team2Kad), true);
            builder.AddField($"rating ({PlayerStats.Team2[0].RatingVersion})", string.Join("\n", team2Rating), true);

            builder.WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }
}