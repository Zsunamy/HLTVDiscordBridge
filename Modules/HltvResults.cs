using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using MongoDB.Driver;
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
        private static async Task<List<MatchResult>> GetMatchResultsOfEvent(List<uint> eventIds)
        {
            List<string> eventIdsString = new();
            foreach(uint eventId in eventIds)
            {
                eventIdsString.Add(eventId.ToString());
            }
            List<List<string>> values = new(); values.Add(eventIdsString);
            List<string> properties = new(); properties.Add("eventIds");
            JArray req = await Tools.RequestApiJArray("getResults", properties, values);

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
        private static async Task<List<MatchResult>> GetAllResults()
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("startDate"); properties.Add("endDate");
            string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddDays(-2));
            string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
            values.Add(startDate); values.Add(endDate);

            var req = await Tools.RequestApiJArray("getResults", properties, values);
            
            Directory.CreateDirectory("./cache/results");

            List<MatchResult> results = new();
            
            foreach (JToken jTok in req)
            {
                results.Add(new MatchResult(jTok as JObject));
            }
            
            return results;
        }
        private static async Task<List<MatchResult>> GetNewMatchResults()
        {
            List<MatchResult> newResults = await GetAllResults();

            List<MatchResult> oldResults = new();
            var oldResultsJArray = JArray.Parse(File.ReadAllText("./cache/results/results.json"));
            foreach (JToken jToken in oldResultsJArray)
            {
                JObject jObj = JObject.Parse(jToken.ToString());
                MatchResult oldResult = new(jObj);
                oldResults.Add(oldResult);
            }
            List<MatchResult> results = new();
            foreach (var newResult in newResults)
            {
                var found = false;
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
                }
            }
            if (!results.Any())
            {
                return null;
            }
            File.WriteAllText("./cache/results/results.json", JArray.FromObject(newResults).ToString());
            return results;
        }
        private static Embed GetResultEmbed(MatchResult matchResult, Match match)
        {
            EmbedBuilder builder = new();
            string title;
            if (match.winnerTeam.name == match.team1.name)
            {
                title = $"👑 {match.team1.name} vs. {match.team2.name}";
            }
            else
            {
                title = $"{match.team1.name} vs. {match.team2.name} 👑";
            }
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

                Match newMatch = newMatches.ElementAt(newMatchResults.IndexOf(matchResult));
                // ReSharper disable once PossibleInvalidOperationException
                List<(ulong, string)> webhooks = (
                    from config in await Config.GetServerConfigs(x => x.ResultWebhookId != null && x.MinimumStars <= matchResult.stars)
                    select ((ulong)config.ResultWebhookId, config.ResultWebhookToken)).ToList();

                await Tools.SendMessagesWithWebhook(webhooks, GetResultEmbed(matchResult, newMatch),GetMessageComponent(newMatch));
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
        private static string SpliceText(string text, int lineLength)
        {
            var charCount = 0;
            var lines = text.Split(new [] { " " }, StringSplitOptions.RemoveEmptyEntries)
                            .GroupBy(w => (charCount += w.Length + 1) / lineLength)
                            .Select(g => string.Join(" ", g));

            return String.Join("\n", lines.ToArray());
        }
    }
}
