using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvResults : ModuleBase<SocketCommandContext>
    {
        public static async Task<JObject> GetMatchByMatchId(uint matchId)
        {
            var URI = new Uri("http://revilum.com:3000/api/match/" + matchId);
            HttpClient http = new();
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            return JObject.Parse(httpRes);
        }

        #region Results
        public static async Task<JArray> GetResults(ushort teamId)
        {
            string idsString = $"[{teamId}]";
            var URI = new Uri($"http://revilum.com:3000/api/results/teams/{idsString}");
            HttpClient http = new();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            return JArray.Parse(httpRes);
        }
        public static async Task<JArray> GetUpcomingMatches(ushort teamId)
        {
            string idsString = $"[{teamId}]";
            var URI = new Uri($"http://revilum.com:3000/api/matches/teams/{idsString}");
            HttpClient http = new();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            return JArray.Parse(httpRes);
        }
        /// <summary>
        /// Updates the results if there is a new one.
        /// </summary>
        /// <returns>The latest results as JArray</returns>
        private static async Task<JArray> UpdateResultsCache()
        {
            //var URI = new Uri("https://hltv-api-steel.vercel.app/api/results");
            var URI = new Uri("http://revilum.com:3000/api/results");
            HttpClient http = new();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();            
            JArray jArr;
            try { jArr = JArray.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return null; }
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
            if (!File.Exists("./cache/results/results.json")) { await UpdateResultsCache(); return null; }
            JArray oldResults = JArray.Parse(File.ReadAllText("./cache/results/results.json"));
            JArray newResults = await UpdateResultsCache();
            if(newResults == null) { return null; }
            else if (oldResults.ToString() == newResults.ToString()) { return null; }
            foreach(JObject jObj in newResults)
            {
                bool newResult = true;
                foreach(JObject kObj in oldResults)
                {
                    if(jObj.ToString() == kObj.ToString()) { newResult = false; }
                }
                if(newResult)
                {
                    await Task.Delay(5000);
                    //var URI = new Uri("https://hltv-api-steel.vercel.app/api/match/" + matchId);
                    var URI = new Uri("http://revilum.com:3000/api/match/" + jObj.GetValue("id").ToString());
                    HttpClient http = new();
                    HttpResponseMessage httpResponse = await http.GetAsync(URI);
                    string httpRes = await httpResponse.Content.ReadAsStringAsync();
                    JObject matchStats = JObject.Parse(httpRes);
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
            string latestMatchId = $"https://www.hltv.org/matches/{res.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-')}-vs-" +
                $"{team2.GetValue("name").ToString().Replace(' ', '-')}-{eventInfo.GetValue("name").ToString().Replace(' ', '-')}";

            EmbedBuilder builder = new();
            builder.WithTitle($"{team1.GetValue("name")} vs. {team2.GetValue("name")}")
                .WithColor(Color.Red)
                .AddField("event:", $"[{eventInfo.GetValue("name")}]({eventLink})\n{additionalInfo}")
                .AddField("winner:", $"[{winner.GetValue("name")}]({winnerLink})", true)
                .AddField("format:", $"{GetFormatFromAcronym(format.GetValue("type").ToString())} ({format.GetValue("location")})", true)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", latestMatchId)
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
                    mapsString += $"__{GetMapNameByAcronym(mapName)}__\n";
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
            Config _cfg = new();
            List<(JObject, ushort)> newMatches = await GetNewMatches();
            if(newMatches != null)
            {
                foreach ((JObject, ushort) match in newMatches)
                {
                    JObject latestMatch = match.Item1;
                    ushort stars = match.Item2;
                    foreach (SocketTextChannel channel in await _cfg.GetChannels(client))
                    {
                        if (_cfg.GetServerConfig(channel).MinimumStars <= stars)
                        {
                            try { RestUserMessage msg = await channel.SendMessageAsync(embed: GetResultEmbed(latestMatch, stars)); await msg.AddReactionAsync(await Config.GetEmote(client)); }
                            catch (Discord.Net.HttpException) { Console.WriteLine($"not enough permission in channel {channel}"); }
                        }
                    }
                }
            }
        }
        #endregion

        #region PlayerStatsOfResult
        private static async Task<JObject> GetPlStats(string matchid)
        {
            //var URI = new Uri("https://hltv-api-steel.vercel.app/api/match/" + matchid);
            var URI = new Uri("http://revilum.com:3000/api/match/" + matchid);
            HttpClient http = new();
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpResPLStats = await httpResponse.Content.ReadAsStringAsync();
            JObject match = JObject.Parse(httpResPLStats);

            URI = new Uri("http://revilum.com:3000/api/matchstats/" + match.GetValue("statsId"));
            httpResponse = await http.GetAsync(URI);
            string matchStats = await httpResponse.Content.ReadAsStringAsync();
            JObject matchStatsObj = JObject.Parse(matchStats);

            return matchStatsObj;
        }
        private static async Task<Embed> GetPlStatsEmbed(string matchlink)
        {
            EmbedBuilder builder = new();
            
            JObject matchStats = await GetPlStats(matchlink.Substring(29, 7));
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
        public static async Task SendPlStats(string matchLink, ITextChannel channel)
        {
            await channel.SendMessageAsync(embed: await GetPlStatsEmbed(matchLink));
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
                _ => arg[0].ToString().ToUpper() + arg.Substring(1),
            };
        }
        private static string DoNewLines(string req, int charactersPerLine)
        {
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