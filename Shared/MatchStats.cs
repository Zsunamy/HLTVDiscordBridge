﻿using System.Collections.Generic;
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
    public MatchStatsPlayerTeams MatchStatsPlayerTeams { get; set; }
    public string Link { get; set; }
    
    public Embed ToEmbed()
        {
            EmbedBuilder builder = new();

            builder.WithTitle($"PLAYERSTATS ({Team1.Name} vs. {Team2.Name})")
                .WithColor(Color.Red);

            List<string> team1PlayerNames = new();
            List<string> team1KAD = new();
            List<string> team1Rating = new();
            foreach (MatchStatsPlayer playerStats in MatchStatsPlayerTeams.Team1PlayerStats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.Player.Id}/{playerStats.Player.Name.ToLower().Replace(' ', '-')}";
                team1PlayerNames.Add($"[{playerStats.Player.Name}]({playerLink})");
                team1KAD.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team1Rating.Add(playerStats.Rating1.ToString(CultureInfo.InvariantCulture));
            }
            builder.AddField($"players ({Team1.Name}):", string.Join("\n", team1PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team1KAD), true);
            builder.AddField("rating", string.Join("\n", team1Rating), true);

            List<string> team2PlayerNames = new();
            List<string> team2Kad = new();
            List<string> team2Rating = new();
            foreach (MatchStatsPlayer playerStats in MatchStatsPlayerTeams.Team2PlayerStats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.Player.Id}/{playerStats.Player.Name.ToLower().Replace(' ', '-')}";
                team2PlayerNames.Add($"[{playerStats.Player.Name}]({playerLink})");
                team2Kad.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team2Rating.Add(playerStats.Rating1.ToString(CultureInfo.CurrentCulture));
            }
            builder.AddField($"players ({Team2.Name}):", string.Join("\n", team2PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team2Kad), true);
            builder.AddField("rating", string.Join("\n", team2Rating), true);

            builder.WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", Link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }
}