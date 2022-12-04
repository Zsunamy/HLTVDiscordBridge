using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Modules
{
    public static class HltvResults
    {
        public static async Task<List<MatchResult>> GetMatchResultsOfEvent(uint eventId)
        {
            List<uint> eventIds = new() { eventId };
            return await GetMatchResultsOfEvent(eventIds);
        }
        private static async Task<List<MatchResult>> GetMatchResultsOfEvent(IEnumerable<uint> eventIds)
        {
            List<string> eventIdsString = eventIds.Select(eventId => eventId.ToString()).ToList();
            List<List<string>> values = new() { eventIdsString };
            List<string> properties = new() { "eventIds" };
            JArray req = await Tools.RequestApiJArray("getResults", properties, values);

            return req.Select(matchResult => new MatchResult(matchResult as JObject)).ToList();
        }
        public static async Task<List<MatchResult>> GetMatchResults(uint teamId)
        {
            List<string> teamIds = new() { teamId.ToString() };

            List<List<string>> values = new() { teamIds };
            List<string> properties = new() { "teamIds" };

            JArray req = await Tools.RequestApiJArray("getResults", properties, values);

            return req.Select(result => new MatchResult(JObject.Parse(result.ToString()))).ToList();
        }
        private static async Task<List<MatchResult>> GetAllResults()
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("startDate"); properties.Add("endDate");
            string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddDays(-2));
            string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
            values.Add(startDate); values.Add(endDate);

            JArray req = await Tools.RequestApiJArray("getResults", properties, values);
            
            Directory.CreateDirectory("./cache/results");

            return req.Select(jTok => new MatchResult(jTok as JObject)).ToList();
        }
        private static async Task<List<MatchResult>> GetNewMatchResults()
        {
            List<MatchResult> newResults = await GetAllResults();

            JArray oldResultsJArray = JArray.Parse(await File.ReadAllTextAsync("./cache/results/results.json"));
            List<MatchResult> oldResults = oldResultsJArray.Select(jToken => JObject.Parse(jToken.ToString()))
                .Select(jObj => new MatchResult(jObj)).ToList();
            List<MatchResult> results = (from newResult in newResults
                let found = oldResults.Any(oldResult => newResult.id == oldResult.id) where !found select newResult).ToList();
            
            await File.WriteAllTextAsync("./cache/results/results.json", JArray.FromObject(newResults).ToString());
            return results;
        }
        private static Embed GetResultEmbed(MatchResult matchResult, Match match)
        {
            EmbedBuilder builder = new();
            string title = match.winnerTeam.name == match.team1.name ? $"👑 {match.team1.name} vs. {match.team2.name}" :
                $"{match.team1.name} vs. {match.team2.name} 👑";
            builder.WithTitle(title)
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
                    string mapHalfResultString = 
                        map.mapResult.mapHalfResults.Aggregate("", (current, mapHalfResult) => current + (current == "" ? $"{mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}" : $" | {mapHalfResult.team1Rounds}:{mapHalfResult.team2Rounds}"));
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
                string highlightsString = highlights.Aggregate
                    ("", (current, highlight) => current + $"[{SpliceText(highlight.title, 35)}]({highlight.link})\n\n");
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
        public static async Task SendNewResults()
        {
            Stopwatch watch = new();
            watch.Start();
            List<MatchResult> newMatchResults = await GetNewMatchResults();
            if (newMatchResults == null)
            {
                return;
            }

            List<Match> newMatches = new();
            foreach (MatchResult matchResult in newMatchResults)
            {
                newMatches.Add(await HltvMatch.GetMatch(matchResult));
            }

            foreach (MatchResult matchResult in newMatchResults)
            {

                Match newMatch = newMatches.ElementAt(newMatchResults.IndexOf(matchResult));
                await Tools.SendMessagesWithWebhook(x => x.ResultWebhookId != null && x.MinimumStars >= matchResult.stars,
                    x => x.ResultWebhookId, x => x.ResultWebhookToken, GetResultEmbed(matchResult, newMatch),
                    GetMessageComponent(newMatch));
            }
            Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched results ({watch.ElapsedMilliseconds}ms)");
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
                _ => arg[0].ToString().ToUpper() + arg[1..],
            };
        }
        private static string SpliceText(string text, int lineLength)
        {
            int charCount = 0;
            IEnumerable<string> lines = text.Split(new [] { " " }, StringSplitOptions.RemoveEmptyEntries)
                            .GroupBy(w => (charCount += w.Length + 1) / lineLength)
                            .Select(g => string.Join(" ", g));

            return string.Join("\n", lines.ToArray());
        }
    }
}
