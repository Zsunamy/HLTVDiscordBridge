using Discord;
using HLTVDiscordBridge.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvMatchStats
    {
        public static async Task<MatchStats> GetMatchStats(Match match)
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("id"); values.Add(match.statsId.ToString());            
            return new MatchStats(await Tools.RequestApiJObject("getMatchStats", properties, values));
        }
        public static Embed GetPlayerStatsEmbed(MatchStats matchStats)
        {
            EmbedBuilder builder = new();

            builder.WithTitle($"PLAYERSTATS ({matchStats.team1.name} vs. {matchStats.team2.name})")
                .WithColor(Color.Red);

            List<string> team1PlayerNames = new();
            List<string> team1KAD = new();
            List<string> team1Rating = new();
            foreach (MatchStatsPlayer playerStats in matchStats.matchStatsPlayerTeams.team1PlayerStats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.player.id}/{playerStats.player.name.ToLower().Replace(' ', '-')}";
                team1PlayerNames.Add($"[{playerStats.player.name}]({playerLink})");
                team1KAD.Add($"{playerStats.kills}/{playerStats.assists}/{playerStats.deaths}");
                team1Rating.Add(playerStats.rating1.ToString());
            }
            builder.AddField($"players ({matchStats.team1.name}):", string.Join("\n", team1PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team1KAD), true);
            builder.AddField("rating", string.Join("\n", team1Rating), true);

            List<string> team2PlayerNames = new();
            List<string> team2KAD = new();
            List<string> team2Rating = new();
            foreach (MatchStatsPlayer playerStats in matchStats.matchStatsPlayerTeams.team2PlayerStats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.player.id}/{playerStats.player.name.ToLower().Replace(' ', '-')}";
                team2PlayerNames.Add($"[{playerStats.player.name}]({playerLink})");
                team2KAD.Add($"{playerStats.kills}/{playerStats.assists}/{playerStats.deaths}");
                team2Rating.Add(playerStats.rating1.ToString());
            }
            builder.AddField($"players ({matchStats.team2.name}):", string.Join("\n", team2PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team2KAD), true);
            builder.AddField("rating", string.Join("\n", team2Rating), true);

            builder.WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", matchStats.link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }
        public static Embed GetPlayerStatsEmbed(MatchMapStats matchMapStats)
        {
            EmbedBuilder builder = new();

            builder.WithTitle($"PLAYERSTATS ({matchMapStats.team1.name} vs. {matchMapStats.team2.name})")
                .WithColor(Color.Red);
            Console.WriteLine(matchMapStats.team1stats.Count);
            List<string> team1PlayerNames = new();
            List<string> team1KAD = new();
            List<string> team1Rating = new();
            foreach (MatchMapStatsPlayer playerStats in matchMapStats.team1stats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.player.id}/{playerStats.player.name.ToLower().Replace(' ', '-')}";
                team1PlayerNames.Add($"[{playerStats.player.name}]({playerLink})");
                team1KAD.Add($"{playerStats.kills}/{playerStats.assists}/{playerStats.deaths}");
                team1Rating.Add(playerStats.rating1.ToString());
            }
            builder.AddField($"players ({matchMapStats.team1.name}):", string.Join("\n", team1PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team1KAD), true);
            builder.AddField("rating", string.Join("\n", team1Rating), true);

            List<string> team2PlayerNames = new();
            List<string> team2KAD = new();
            List<string> team2Rating = new();
            foreach (MatchMapStatsPlayer playerStats in matchMapStats.team2stats)
            {
                string playerLink = $"https://hltv.org/player/{playerStats.player.id}/{playerStats.player.name.ToLower().Replace(' ', '-')}";
                team2PlayerNames.Add($"[{playerStats.player.name}]({playerLink})");
                team2KAD.Add($"{playerStats.kills}/{playerStats.assists}/{playerStats.deaths}");
                team2Rating.Add(playerStats.rating1.ToString());
            }
            builder.AddField($"players ({matchMapStats.team2.name}):", string.Join("\n", team2PlayerNames), true);
            builder.AddField("K/A/D", string.Join("\n", team2KAD), true);
            builder.AddField("rating", string.Join("\n", team2Rating), true);

            builder.WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", matchMapStats.link);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }
    }
}
