using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvResults_old : ModuleBase<SocketCommandContext>
    {
        public static async Task<JObject> GetMatchByMatchId(uint matchId)
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("id");
            values.Add(matchId.ToString());
            var req = await Tools.RequestApiJObject("getMatch", properties, values);
            return req;
        }

        #region Results
        public static async Task<JArray> GetResults(ushort teamId)
        {
            List<string> teamIds = new(); teamIds.Add(teamId.ToString());
            List<List<string>> values = new();  values.Add(teamIds);
            List<string> properties = new(); properties.Add("teamIds");
            var req = await Tools.RequestApiJArray("getResults", properties, values);
            return req;
        }
        
        /// <summary>
        /// Updates the results if there is a new one.
        /// </summary>
        /// <returns>The latest results as JArray</returns>
        public static async Task<JArray> UpdateResultsCache()
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
            JArray jArr = req;

            Directory.CreateDirectory("./cache/results");
            Directory.CreateDirectory("./archive/results");
            if(!File.Exists("./cache/results/results.json")) { File.WriteAllText("./cache/results/results.json", jArr.ToString()); }
            JArray oldResults = JArray.Parse(File.ReadAllText("./cache/results/results.json"));
            if(oldResults != jArr) { File.WriteAllText("./cache/results/results.json", jArr.ToString()); }            
            return jArr;
        }
        /// <summary>
        /// Gets the stats of the latest match if it was not cached
        /// </summary>
        /// <returns>Latest Match Stats as JObject, star rating of the match as ushort</returns>
        private static async Task<List<(JObject, ushort)>> GetNewMatches()
        {
            List<(JObject, ushort)> newMatches = new();
            Directory.CreateDirectory("./cache/results");
            if (!File.Exists("./cache/results/results.json")) { await UpdateResultsCache(); await GetNewMatches(); return null; }
            JArray oldResults = JArray.Parse(File.ReadAllText("./cache/results/results.json"));
            JArray newResults = await UpdateResultsCache();
            if(newResults == null || oldResults.ToString() == newResults.ToString()) { return null; }

            foreach(JObject jObj in newResults)
            {
                if(!File.Exists("./cache/results/resultIds.txt")) { File.WriteAllText("./cache/results/resultIds.txt", jObj.GetValue("id").ToString() + "\n"); }
                else 
                {
                    string[] ids = File.ReadAllLines("./cache/results/resultIds.txt");
                    bool alreadysent = false;
                    foreach(string id in ids)
                    {
                        if(id == jObj.GetValue("id").ToString()) { alreadysent = true; }
                    }
                    if(alreadysent) { continue; }
                    File.WriteAllText("./cache/results/resultIds.txt", jObj.GetValue("id").ToString() + "\n" + File.ReadAllText("./cache/results/resultIds.txt")); 
                }
                bool newResult = true;
                foreach(JObject kObj in oldResults)
                {
                    if(jObj.ToString() == kObj.ToString()) { newResult = false; }
                }
                if(newResult)
                {
                    await Task.Delay(5000);
                    List<string> properties = new();
                    List<string> values = new();
                    properties.Add("id");
                    values.Add(jObj.GetValue("id").ToString());
                    var req = await Tools.RequestApiJObject("getMatch", properties, values);
                    JObject matchStats = req;
                    newMatches.Add((matchStats, ushort.Parse(jObj.GetValue("stars").ToString())));
                }
            }
            return newMatches;            
        }
        private static Embed GetResultEmbed(JObject res, ushort stars)
        {            
            
            JObject eventInfo = JObject.Parse(res.GetValue("event").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventInfo.GetValue("id")}/{eventInfo.GetValue("name").ToString().Replace(' ', '-')}";
            string additionalInfo = "";
            if(res.TryGetValue("significance", out JToken val)) { additionalInfo = val.ToString(); }
            JObject team1 = JObject.Parse(res.GetValue("team1").ToString());
            JObject team2 = JObject.Parse(res.GetValue("team2").ToString());
            JObject winner = JObject.Parse(res.GetValue("winnerTeam").ToString());
            string winnerLink = $"https://www.hltv.org/team/{winner.GetValue("id")}/{winner.GetValue("name").ToString().Replace(' ', '-')}";
            JArray maps = JArray.Parse(res.GetValue("maps").ToString());
            JArray highlights = JArray.Parse(res.GetValue("highlights").ToString());
            JObject format = JObject.Parse(res.GetValue("format").ToString()); 
            string link = $"https://www.hltv.org/matches/{res.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-')}-vs-" +
                $"{team2.GetValue("name").ToString().Replace(' ', '-')}-{eventInfo.GetValue("name").ToString().Replace(' ', '-')}";

            EmbedBuilder builder = new();
            builder.WithTitle($"{team1.GetValue("name")} vs. {team2.GetValue("name")}")
                .WithColor(Color.Red)
                .AddField("event:", $"[{eventInfo.GetValue("name")}]({eventLink})\n{additionalInfo}")
                .AddField("winner:", $"[{winner.GetValue("name")}]({winnerLink})", true)
                .AddField("format:", $"{GetFormatFromAcronym(format.GetValue("type").ToString())} ({format.GetValue("location")})", true)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", link)
                .WithCurrentTimestamp();
            string footerString = "";
            Emoji emo = new("⭐");
            for (int i = 1; i <= stars; i++)
            {
                footerString += emo;
            }
            builder.WithFooter(footerString);

            string mapsString = "";
            foreach (JObject map in maps)
            {
                string mapName = map.GetValue("name").ToString();
                if(map.TryGetValue("result", out JToken resultTok))
                {
                    JObject result = JObject.Parse(resultTok.ToString());
                    JArray halfResults = JArray.Parse(result.GetValue("halfResults").ToString());
                    string halfResultsString = "";
                    for (int i = 0; i < halfResults.Count; i++)
                    {
                        JObject halfResult = JObject.Parse(halfResults[i].ToString());
                        if (i == 0) { halfResultsString += $"{halfResult.GetValue("team1Rounds")}:{halfResult.GetValue("team2Rounds")}"; continue; }
                        halfResultsString += $" | {halfResult.GetValue("team1Rounds")}:{halfResult.GetValue("team2Rounds")}";
                    }
                    mapsString += $"{GetMapNameByAcronym(mapName)} ({result.GetValue("team1TotalRounds")}:{result.GetValue("team2TotalRounds")}) ({halfResultsString})\n";
                } else
                {
                    mapsString += $"~~{GetMapNameByAcronym(mapName)}~~\n";
                }
                
            }
            builder.AddField("maps:", mapsString);
            
            if(highlights.Count > 0)
            {
                string highlightsString = "";
                for (int i = 0; i < highlights.Count; i++)
                {
                    if (i == 2) { break; }
                    JObject highlight = JObject.Parse(highlights[i].ToString());
                    highlightsString += $"[{DoNewLines(highlight.GetValue("title").ToString(), 35)}]({highlight.GetValue("link")})\n\n";
                }
                builder.AddField("highlights:", highlightsString);
            }   
            return builder.Build();
        }

        public static async Task AktResults(DiscordSocketClient client)
        {
            List<(JObject, ushort)> newMatches = await GetNewMatches();
            if(newMatches != null)
            {
                foreach ((JObject, ushort) match in newMatches)
                {
                    StatsUpdater.StatsTracker.MatchesSent += 1;
                    StatsUpdater.UpdateStats();

                    JObject latestMatch = match.Item1;
                    ushort stars = match.Item2;
                    foreach (SocketTextChannel channel in await Config.GetChannels(client))
                    {
                        ServerConfig config = Config.GetServerConfig(channel);
                        if (config.MinimumStars <= stars && config.ResultOutput)
                        {
                            try {
                                ComponentBuilder compBuilder = new ComponentBuilder();
                                compBuilder.WithButton("playerstats", "playerstats");
                                RestUserMessage msg = await channel.SendMessageAsync(embed: GetResultEmbed(latestMatch, stars), components: compBuilder.Build());
                                    
                                StatsUpdater.StatsTracker.MessagesSent += 1;
                                StatsUpdater.UpdateStats();
                            }
                            catch (Discord.Net.HttpException) { Program.WriteLog($"not enough permission in channel {channel}"); }
                        }
                    }
                }
            }
        }
        #endregion

        #region PlayerStatsOfResult
        private static async Task<JObject> GetPlStats(string matchId)
        {
            List<string> properties = new();
            List<string> values = new();
            properties.Add("id");
            values.Add(matchId.ToString());
            var req = await Tools.RequestApiJObject("getMatch", properties, values);
            JObject match = req;
            values.Clear(); values.Add(match.GetValue("statsId").ToString());

            req = await Tools.RequestApiJObject("getMatchStats", properties, values);
            return req;
        }
        public static async Task<Embed> GetPlStatsEmbed(string matchlink)
        {
            EmbedBuilder builder = new();
            
            JObject matchStats = await GetPlStats(matchlink.Substring(29, 7));
            if(matchStats == null)
            {
                builder.WithColor(Color.Red)
                    .WithTitle($"error")
                    .WithDescription("Our API is currently not available! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues). We're sorry for the inconvience")
                    .WithCurrentTimestamp();
                return builder.Build();
            }
            JObject PlayerStats = JObject.Parse(matchStats.GetValue("playerStats").ToString());
            JObject team1 = JObject.Parse(matchStats.GetValue("team1").ToString());
            JObject team2 = JObject.Parse(matchStats.GetValue("team2").ToString());
            JArray team1Player = JArray.Parse(PlayerStats.GetValue("team1").ToString());
            JArray team2Player = JArray.Parse(PlayerStats.GetValue("team2").ToString());
            string statsLink = $"https://www.hltv.org/stats/matches/{matchStats.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-').ToLower()}-vs-{team2.GetValue("name").ToString().Replace(' ', '-').ToLower()}";

            builder.WithTitle($"PLAYERSTATS ({team1.GetValue("name")} vs. {team2.GetValue("name")})")
                .WithColor(Color.Red);

            string team1PlayersString = "";
            string team1KADString = "";
            string team1RatingString = "";
            foreach(JObject jObj in team1Player)
            {
                JObject player = JObject.Parse(jObj.GetValue("player").ToString());
                string playerLink = $"https://hltv.org/player/{player.GetValue("id")}/{player.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                team1PlayersString += $"[{player.GetValue("name")}]({playerLink})\n";
                team1KADString += $"{jObj.GetValue("kills")}/{jObj.GetValue("assists")}/{jObj.GetValue("deaths")}\n";
                team1RatingString += $"{jObj.GetValue("rating1")}\n";
            }
            builder.AddField($"players ({team1.GetValue("name")}):", team1PlayersString, true);
            builder.AddField("K/A/D", team1KADString, true);
            builder.AddField("rating", team1RatingString, true);

            string team2PlayersString = "";
            string team2KADString = "";
            string team2RatingString = "";
            foreach (JObject jObj in team2Player)
            {
                JObject player = JObject.Parse(jObj.GetValue("player").ToString());
                string playerLink = $"https://hltv.org/player/{player.GetValue("id")}/{player.GetValue("name").ToString().ToLower().Replace(' ', '-')}";
                team2PlayersString += $"[{player.GetValue("name")}]({playerLink})\n";
                team2KADString += $"{jObj.GetValue("kills")}/{jObj.GetValue("assists")}/{jObj.GetValue("deaths")}\n";
                team2RatingString += $"{jObj.GetValue("rating1")}\n";
            }
            builder.AddField($"players ({team2.GetValue("name")}):", team2PlayersString, true);
            builder.AddField("K/A/D", team2KADString, true);
            builder.AddField("rating", team2RatingString, true);

            builder.WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", statsLink);
            builder.WithCurrentTimestamp();

            return builder.Build();
        }
        #endregion

        #region tools
        private static string GetFormatFromAcronym(string req)
        {
            return req switch
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
        private static string DoNewLines(string req, int charactersPerLine)
        {
            if(!req.Contains(" | ")) { return req; }
            req = req.Split(" | ")[1];
            if (req.Length > charactersPerLine && req.Length <= charactersPerLine * 2) { req = $"{req.Substring(0, req.Substring(charactersPerLine).IndexOf(' ') + charactersPerLine)}\n{req.Substring(req.Substring(charactersPerLine).IndexOf(' ') + charactersPerLine)}"; }
            else if (req.Length > charactersPerLine * 2)
            {
                req = $"{req.Substring(0, req.Substring(charactersPerLine).IndexOf(' ') + charactersPerLine)}\n" +
                         $"{req.Substring(req.Substring(charactersPerLine).IndexOf(' ') + charactersPerLine, req.Substring(charactersPerLine * 2).IndexOf(' ') + req.Substring(charactersPerLine).IndexOf(' ') + charactersPerLine)}\n" +
                         $"{req.Substring(req.Substring(charactersPerLine * 2).IndexOf(' ') + req.Substring(charactersPerLine).IndexOf(' ') + charactersPerLine + req.Substring(charactersPerLine).IndexOf(' ') + charactersPerLine)}";
            }
            return req;
        }
        #endregion
    }
}