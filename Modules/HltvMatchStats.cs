using Discord;
using HLTVDiscordBridge.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Modules
{
    public static class HltvMatchStats
    {
        public static async Task<MatchStats> GetMatchStats(Match match)
        {
            GetMatchStats request = new GetMatchStats{Id = match.StatsId};
            return await request.SendRequest<MatchStats>();
        }
        public static Embed GetPlayerStatsEmbed(MatchStats matchStats)
        {
            EmbedBuilder builder = new();

            builder.WithTitle($"PLAYERSTATS ({matchStats.Team1.Name} vs. {matchStats.Team2.Name})")
                .WithColor(Color.Red);

            List<string> team1PlayerNames = new();
            List<string> team1KAD = new();
            List<string> team1Rating = new();
            foreach (MatchStatsPlayer playerStats in matchStats.MatchStatsPlayerTeams.Team1PlayerStats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.Player.Id}/{playerStats.Player.Name.ToLower().Replace(' ', '-')}";
                team1PlayerNames.Add($"[{playerStats.Player.Name}]({playerLink})");
                team1KAD.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team1Rating.Add(playerStats.Rating1.ToString());
            }
            builder.AddField($"players ({matchStats.Team1.Name}):", string.Join("\n", team1PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team1KAD), true);
            builder.AddField("rating", string.Join("\n", team1Rating), true);

            List<string> team2PlayerNames = new();
            List<string> team2KAD = new();
            List<string> team2Rating = new();
            foreach (MatchStatsPlayer playerStats in matchStats.MatchStatsPlayerTeams.Team2PlayerStats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.Player.Id}/{playerStats.Player.Name.ToLower().Replace(' ', '-')}";
                team2PlayerNames.Add($"[{playerStats.Player.Name}]({playerLink})");
                team2KAD.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team2Rating.Add(playerStats.Rating1.ToString());
            }
            builder.AddField($"players ({matchStats.Team2.Name}):", string.Join("\n", team2PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team2KAD), true);
            builder.AddField("rating", string.Join("\n", team2Rating), true);

            builder.WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", matchStats.Link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }
        public static Embed GetPlayerStatsEmbed(MatchMapStats matchMapStats)
        {
            EmbedBuilder builder = new();

            builder.WithTitle($"PLAYERSTATS ({matchMapStats.Team1.Name} vs. {matchMapStats.Team2.Name})")
                .WithColor(Color.Red);
            Console.WriteLine(matchMapStats.Team1Stats.Count);
            List<string> team1PlayerNames = new();
            List<string> team1KAD = new();
            List<string> team1Rating = new();
            foreach (MatchMapStatsPlayer playerStats in matchMapStats.Team1Stats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.Player.Id}/{playerStats.Player.Name.ToLower().Replace(' ', '-')}";
                team1PlayerNames.Add($"[{playerStats.Player.Name}]({playerLink})");
                team1KAD.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team1Rating.Add(playerStats.Rating1.ToString());
            }
            builder.AddField($"players ({matchMapStats.Team1.Name}):", string.Join("\n", team1PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team1KAD), true);
            builder.AddField("rating", string.Join("\n", team1Rating), true);

            List<string> team2PlayerNames = new();
            List<string> team2KAD = new();
            List<string> team2Rating = new();
            foreach (MatchMapStatsPlayer playerStats in matchMapStats.Team2Stats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.Player.Id}/{playerStats.Player.Name.ToLower().Replace(' ', '-')}";
                team2PlayerNames.Add($"[{playerStats.Player.Name}]({playerLink})");
                team2KAD.Add($"{playerStats.Kills}/{playerStats.Assists}/{playerStats.Deaths}");
                team2Rating.Add(playerStats.Rating1.ToString());
            }
            builder.AddField($"players ({matchMapStats.Team2.Name}):", string.Join("\n", team2PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team2KAD), true);
            builder.AddField("rating", string.Join("\n", team2Rating), true);

            builder.WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", matchMapStats.Link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }
    }
}
