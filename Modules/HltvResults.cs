using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Modules
{
    public class HltvResults
    {
        public static async Task<List<MatchResult>> GetMatchResultsOfEvent(uint eventId)
        {
            List<uint> eventIds = new();
            eventIds.Add(eventId);
            return await GetMatchResultsOfEvent(eventIds);
        }
        public static async Task<List<MatchResult>> GetMatchResultsOfEvent(List<uint> eventIds)
        {
            List<string> eventIdsString = new();
            foreach(uint eventId in eventIds)
            {
                eventIdsString.Add(eventId.ToString());
            }
            List<List<string>> values = new(); values.Add(eventIdsString);
            List<string> properties = new(); properties.Add("eventIds");
            var req = await Tools.RequestApiJArray("getResults", properties, values);

            List<MatchResult> matchResults = new();
            foreach(JToken matchResult in req)
            {
                matchResults.Add(new MatchResult(matchResult as JObject));
            }

            return matchResults;
        }
        public static async Task<List<MatchResult>> GetMatchResults(uint teamId)
        {
            List<string> teamIds = new(); teamIds.Add(teamId.ToString());

            List<List<string>> values = new(); values.Add(teamIds);
            List<string> properties = new(); properties.Add("teamIds");

            var req = await Tools.RequestApiJArray("getResults", properties, values);

            List<MatchResult> results = new();
            foreach(JToken result in req)
            {
                results.Add(new MatchResult(JObject.Parse(result.ToString())));
            }
            return results;
        }
        public static async Task<List<MatchResult>> GetAllResults()
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("startDate"); properties.Add("endDate");
            string startMonth = DateTime.UtcNow.AddDays(-1).Month.ToString().Length == 1 ? $"0{DateTime.UtcNow.AddDays(-1).Month}" : DateTime.UtcNow.AddDays(-1).Month.ToString();
            string startDay = DateTime.UtcNow.AddDays(-1).Day.ToString().Length == 1 ? $"0{DateTime.UtcNow.AddDays(-1).Day}" : DateTime.UtcNow.AddDays(-1).Day.ToString();
            string endMonth = DateTime.UtcNow.Month.ToString().Length == 1 ? $"0{DateTime.UtcNow.Month}" : DateTime.UtcNow.Month.ToString();
            string endDay = DateTime.UtcNow.Day.ToString().Length == 1 ? $"0{DateTime.UtcNow.Day}" : DateTime.UtcNow.Day.ToString();
            string startDate = $"{DateTime.UtcNow.Year}-{startMonth}-{startDay}";
            string endDate = $"{DateTime.UtcNow.Year}-{endMonth}-{endDay}";
            values.Add(startDate); values.Add(endDate);

            var req = await Tools.RequestApiJArray("getResults", properties, values);

            Directory.CreateDirectory("./archive/results");
            Directory.CreateDirectory("./cache/results");

            List<MatchResult> results = new();
            
            foreach (JToken jTok in req)
            {
                results.Add(new MatchResult(jTok as JObject));
            }
            
            return results;
        }
        public static async Task<List<MatchResult>> GetNewMatchResults()
        {
            List<MatchResult> newResults = await GetAllResults();

            List<MatchResult> oldResults = new();
            JArray oldResultsJArray = JArray.Parse(File.ReadAllText("./cache/results/results.json"));
            foreach (JToken jToken in oldResultsJArray)
            {
                JObject jObj = JObject.Parse(jToken.ToString());
                MatchResult oldResult = new(jObj);
                oldResults.Add(oldResult);
            }
/*
            if (newResults.First().id == oldResults.First().id) { return null; }
            else { File.WriteAllText("./cache/results/results.json", JArray.FromObject(newResults).ToString()); }

            List<MatchResult> results = new();
            foreach(MatchResult newResult in newResults)
            {
                bool isOld = false;
                foreach (MatchResult oldResult in oldResults)
                {
                    if(newResult.id == oldResult.id) { isOld = true; break; }
                }
                if(!isOld) { results.Add(newResult); }
            }*/
            List<MatchResult> results = new();
            var found = false;
            foreach (var newResult in newResults)
            {
                foreach (var oldResult in oldResults)
                {
                    if (newResult.id == oldResult.id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    results.Add(newResult);
                    found = false;
                }

            }
            if (results.Any())
            {
                return null;
            }
            File.WriteAllText("./cache/results/results.json", JArray.FromObject(newResults).ToString());
            return results;
        }
        private static Embed GetResultEmbed(MatchResult matchResult, Match match)
        {
            EmbedBuilder builder = new();
            builder.WithTitle($"{match.team1.name} vs. {match.team2.name}")
                .WithColor(Color.Red)
                .AddField("event:", $"[{match.eventObj.name}]({match.eventObj.link})\n{match.significance}")
                .AddField("winner:", $"[{match.winnerTeam.name}]({match.winnerTeam.link})", true)
                .AddField("format:", $"{GetFormatFromAcronym(match.format.type)} ({match.format.location})", true)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", match.link)
                .WithCurrentTimestamp();
            string footerString = "";
            Emoji emo = new("⭐");
            for (int i = 1; i <= matchResult.stars; i++)
            {
                footerString += emo;
            }
            builder.WithFooter(footerString);

            string mapsString = "";
            foreach(Map map in match.maps)
            {
                if(map.mapResult != null)
                {
                    string mapHalfResultString = "";
                    foreach(MapHalfResult mapHalfResult in map.mapResult.mapHalfResults)
                    {
                        mapHalfResultString += mapHalfResultString == "" ? $"{mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}" : $" | {mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}";                        
                    }
                    mapsString += $"{GetMapNameByAcronym(map.name)} ({map.mapResult.team1TotalRounds}:{map.mapResult.team2TotalRounds}) ({mapHalfResultString})\n";
                }
                else
                {
                    mapsString += $"~~{GetMapNameByAcronym(map.name)}~~\n";
                }
            }
            builder.AddField("maps:", mapsString);
            
            if (match.highlights.Count != 0)
            {
                Highlight[] highlights = new Highlight[2];
                match.highlights.CopyTo(0, highlights, 0, 2);
                string highlightsString = "";
                foreach(Highlight highlight in highlights)
                {
                    highlightsString += $"[{SpliceText(highlight.title, 35)}]({highlight.link})\n\n";
                }
                builder.AddField("highlights:", highlightsString);
            }

            return builder.Build();
        }
        private static MessageComponent GetMessageComponent(Match match)
        {
            ComponentBuilder compBuilder = new();
            switch (match.format.type)
            {
                case "bo1":
                    compBuilder.WithButton("match statistics", "overallstats_bo1");
                    break;
                default:
                    compBuilder.WithButton("match statistics", "overallstats_def");
                    break;
            }
            
            return compBuilder.Build();
        }
        public static async Task SendNewResults(DiscordSocketClient client)
        {
            List<MatchResult> newMatchResults = await GetNewMatchResults();
            if (newMatchResults == null) { return; }

            List<Match> newMatches = new();
            foreach (MatchResult matchResult in newMatchResults)
            {
                newMatches.Add(await HltvMatch.GetMatch(matchResult));
            }

            foreach(MatchResult matchResult in newMatchResults)
            {
                StatsUpdater.StatsTracker.MatchesSent += 1;
                StatsUpdater.UpdateStats();

                Match newMatch = newMatches.ElementAt(newMatchResults.IndexOf(matchResult));

                foreach (SocketTextChannel channel in await Config.GetChannels(client))
                {
                    ServerConfig config = Config.GetServerConfig(channel);
                    if (config.MinimumStars <= matchResult.stars && config.ResultOutput)
                    {
                        try
                        {
                            RestUserMessage msg = await channel.SendMessageAsync(embed: GetResultEmbed(matchResult, newMatch), components: GetMessageComponent(newMatch));
                           
                            StatsUpdater.StatsTracker.MessagesSent += 1;
                            StatsUpdater.UpdateStats();
                        }
                        catch (Discord.Net.HttpException) { Program.WriteLog($"not enough permission in channel {channel}"); }
                        catch (Exception e) {Program.WriteLog(e.ToString());}
                    }
                }
            }
        }
        private static string GetFormatFromAcronym(string arg)
        {
            return arg switch
            {
                "bo1" => "Best of 1",
                "bo3" => "Best of 3",
                "bo5" => "Best of 5",
                "bo7" => "Best of 7",
                _ => "n.A",
            };
        }
        private static string GetMapNameByAcronym(string arg)
        {
            return arg switch
            {
                "tba" => "to be announced",
                "de_train" => "Train",
                "de_cbble" => "Cobble",
                "de_inferno" => "Inferno",
                "de_cache" => "Cache",
                "de_mirage" => "Mirage",
                "de_overpass" => "Overpass",
                "de_dust2" => "Dust 2",
                "de_nuke" => "Nuke",
                "de_tuscan" => "Tuscan",
                "de_vertigo" => "Vertigo",
                "de_season" => "Season",
                "de_ancient" => "Ancient",
                _ => arg[0].ToString().ToUpper() + arg.Substring(1),
            };
        }
        public static string SpliceText(string text, int lineLength)
        {
            var charCount = 0;
            var lines = text.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                            .GroupBy(w => (charCount += w.Length + 1) / lineLength)
                            .Select(g => string.Join(" ", g));

            return String.Join("\n", lines.ToArray());
        }
    }
}
