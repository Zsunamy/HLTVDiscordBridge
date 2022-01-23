using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class MatchStatsPlayerTeams
    {
        public MatchStatsPlayerTeams(JObject jObject)
        {
            if(jObject.TryGetValue("playerStats", out JToken playerStatsTok))
            {
                JObject playerStats = (JObject)playerStatsTok;
                if(playerStats.TryGetValue("team1", out JToken team1Tok))
                {
                    List<MatchStatsPlayer> team1PlayerStats = new();
                    foreach(JToken team1PlayerTok in team1Tok)
                    {
                        team1PlayerStats.Add(new MatchStatsPlayer((JObject)team1PlayerTok));
                    }
                    this.team1PlayerStats = team1PlayerStats;
                } else { team1PlayerStats = null; }

                if (playerStats.TryGetValue("team2", out JToken team2Tok))
                {
                    List<MatchStatsPlayer> team2PlayerStats = new();
                    foreach (JToken team2PlayerTok in team2Tok)
                    {
                        team2PlayerStats.Add(new MatchStatsPlayer((JObject)team2PlayerTok));
                    }
                    this.team2PlayerStats = team2PlayerStats;
                } else { team2PlayerStats = null; }

            }
            else { team1PlayerStats = null; team2PlayerStats = null; }
        }

        public List<MatchStatsPlayer> team1PlayerStats { get; set; }
        public List<MatchStatsPlayer> team2PlayerStats { get; set; }
    }
}
