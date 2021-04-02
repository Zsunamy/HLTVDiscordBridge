using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvResults : ModuleBase<SocketCommandContext>
    {
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
        private static async Task<(JObject, ushort)> GetLatestMatchStats()
        {
            Directory.CreateDirectory("./cache/results");
            if (!File.Exists("./cache/results/results.json")) { await UpdateResultsCache(); return (null, 0); }
            JArray oldResults = JArray.Parse(File.ReadAllText("./cache/results/results.json"));
            JArray newResults = await UpdateResultsCache();
            if(newResults == null) { return (null, 0); }
            else if (oldResults.ToString() == newResults.ToString()) { return (null, 0); }

            string matchId = JObject.Parse(oldResults[0].ToString()).GetValue("id").ToString();
            ushort stars = ushort.Parse(JObject.Parse(oldResults[0].ToString()).GetValue("stars").ToString());

            //var URI = new Uri("https://hltv-api-steel.vercel.app/api/match/" + matchId);
            var URI = new Uri("http://revilum.com:3000/api/match/" + matchId);
            HttpClient http = new();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            return (JObject.Parse(httpRes), stars);
        }
        private static Embed GetResultEmbed(JObject res, ushort stars)
        {            
            JObject eventInfo = JObject.Parse(res.GetValue("event").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventInfo.GetValue("id")}/{eventInfo.GetValue("name").ToString().Replace(' ', '-')}";
            string additionalInfo = "";//res.GetValue("additionalInfo").ToString(); //?
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
            Emoji emo = new Emoji("⭐");
            for (int i = 1; i <= stars; i++)
            {
                footerString += emo;
            }
            builder.WithFooter(footerString);

            string mapsString = "";
            foreach (JToken mapTok in maps)
            {
                JObject map = JObject.Parse(mapTok.ToString());
                string mapName = map.GetValue("name").ToString();
                JObject result = JObject.Parse(map.GetValue("result").ToString());
                JArray halfResults = JArray.Parse(result.GetValue("halfResults").ToString());
                string halfResultsString = "";
                for(int i = 0; i < halfResults.Count; i++)
                {
                    JObject halfResult = JObject.Parse(halfResults[i].ToString());
                    if(i == 0) { halfResultsString += $"{halfResult.GetValue("team1Rounds")}:{halfResult.GetValue("team2Rounds")}"; continue; }
                    halfResultsString += $" | {halfResult.GetValue("team1Rounds")}:{halfResult.GetValue("team2Rounds")}";
                }
                mapsString += $"{GetMapNameByAcronym(mapName)} ({result.GetValue("team1TotalRounds")}:{result.GetValue("team2TotalRounds")}) ({halfResultsString})\n";
            }
            builder.AddField("maps:", mapsString);

            string highlightsString = "";
            for (int i = 0; i < highlights.Count; i++)
            {
                if(i == 2) { break; }
                JObject highlight = JObject.Parse(highlights[i].ToString());
                highlightsString += $"[{DoNewLines(highlight.GetValue("title").ToString(), 35)}]({highlight.GetValue("link")})\n\n";
            }
            builder.AddField("highlights:", highlightsString);

            return builder.Build();
        }
        public static async Task AktResults(DiscordSocketClient client)
        {
            Config _cfg = new();
            var req = await GetLatestMatchStats();
            JObject latestResult = req.Item1;
            ushort stars = req.Item2;
            if (latestResult != null)
            {
                foreach(SocketTextChannel channel in await _cfg.GetChannels(client))
                {
                    if(_cfg.GetServerConfig(channel).MinimumStars <= stars)
                    {
                        try { RestUserMessage msg = await channel.SendMessageAsync(embed: GetResultEmbed(latestResult, stars)); }
                        catch(Discord.Net.HttpException) { Console.WriteLine($"not enough permission in channel {channel}"); }
                    }
                }
            }
        }

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