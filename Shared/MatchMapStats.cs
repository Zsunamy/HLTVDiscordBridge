using System.Collections.Generic;
using System.Globalization;
using Discord;

namespace HLTVDiscordBridge.Shared;

public class MatchMapStats
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public MapResult Result { get; set; }
    public string Map { get; set; }
    public long? Date { get; set; }
    public Team Team1 { get; set; }
    public Team Team2 { get; set; }
    public Event Event { get; set; }
    public MapMatchStatsOverview Overview { get; set; }
    public PlayerStatsMap PlayerStats { get; set; }
    public string Link { get; set; }
    
    public Embed ToEmbed()
    {
        EmbedBuilder builder = new();
        
        builder.WithTitle($"PLAYERSTATS ({Team1.Name} vs. {Team2.Name})")
                .WithColor(Color.Red);
        List<string> team1PlayerNames = new();
        List<string> team1Kad = new();
        List<string> team1Rating = new();
        foreach (MatchMapStatsPlayer playerStats in PlayerStats.Team1)
        {
            string playerLink = $"https://hltv.org/player/{playerStats.Player.Id}/{playerStats.Player.Name.ToLower().Replace(' ', '-')}";
            team1PlayerNames.Add($"[{playerStats.Player.Name}]({playerLink})");
            team1Kad.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
            team1Rating.Add(playerStats.Rating1.ToString(CultureInfo.CurrentCulture));
        }
        builder.AddField($"players ({Team1.Name}):", string.Join("\n", team1PlayerNames), true);
        builder.AddField("K/A/D", string.Join("\n", team1Kad), true);
        builder.AddField("rating", string.Join("\n", team1Rating), true);

        List<string> team2PlayerNames = [];
        List<string> team2Kad = [];
        List<string> team2Rating = [];
        foreach (MatchMapStatsPlayer playerStats in PlayerStats.Team2)
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