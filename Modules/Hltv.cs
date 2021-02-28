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
    public class Hltv : ModuleBase<SocketCommandContext>
    {
        private Config _cfg = new Config();

        public async Task<JObject> GetMatchByMatchId(uint matchId)
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/match/" + matchId);
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            return JObject.Parse(httpRes);
        }

        private async Task<(JObject, ushort)> getLatestMatch()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/results");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr;
            try { jArr = JArray.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return (null, 0); }
            Directory.CreateDirectory("./cache");
            if (!File.Exists("./cache/results.json"))
            {
                var stream = File.Create("./cache/results.json");
                stream.Close();
                File.WriteAllText("./cache/results.json", jArr.ToString());
                return (null, 0);
            }
            JArray results = JArray.Parse(File.ReadAllText("./cache/results.json"));

            foreach (JToken jToken in jArr)
            {
                bool update = true;
                foreach (JToken resultTok in results)
                {
                    if (resultTok.ToString() == jToken.ToString())
                    {
                        update = false;
                    }
                }
                if (update)
                {
                    File.WriteAllText("./cache/results.json", jArr.ToString());
                    string matchId = JObject.Parse(jToken.ToString()).GetValue("id").ToString();
                    var URI1 = new Uri("https://hltv-api-steel.vercel.app/api/match/" + matchId);
                    HttpClient http1 = new HttpClient();
                    http1.BaseAddress = URI1;
                    HttpResponseMessage httpResponse1 = await http1.GetAsync(URI1);
                    string httpRes1 = await httpResponse1.Content.ReadAsStringAsync();

                    return (JObject.Parse(httpRes1), ushort.Parse(JObject.Parse(jToken.ToString()).GetValue("stars").ToString()));
                }
            }
            return (null, 0);
        }

        private string getMapNameByAcronym(string arg)
        {
            switch(arg)
            {
                case "mrg":
                    return "Mirage";
                case "d2":
                    return "Dust 2";
                case "trn":
                    return "Train";
                case "ovp":
                    return "Overpass";
                case "inf":
                    return "Inferno";
                case "nuke":
                    return "Nuke";
                case "vertigo":
                    return "Vertigo";
                default:
                    return arg[0].ToString().ToUpper() + arg.Substring(1);
            }
        }
        /// <summary>
        /// Gets the stats of a match
        /// </summary>
        /// <param name="res">JObject containing the match</param>
        /// <returns>Embed</returns>
        private Embed GetStats(JObject res)
        {
            JObject eventInfo = JObject.Parse(res.GetValue("event").ToString());
            string eventLink = $"https://www.hltv.org/events/{eventInfo.GetValue("id")}/{eventInfo.GetValue("name").ToString().Replace(' ', '-')}";
            string additionalInfo = res.GetValue("additionalInfo").ToString();
            JObject team1 = JObject.Parse(res.GetValue("team1").ToString());
            JObject team2 = JObject.Parse(res.GetValue("team2").ToString());
            JObject winner = JObject.Parse(res.GetValue("winnerTeam").ToString());
            string winnerLink = $"https://www.hltv.org/team/{winner.GetValue("id")}/{winner.GetValue("name").ToString().Replace(' ','-')}";
            JArray maps = JArray.Parse(res.GetValue("maps").ToString());
            JArray highlights = JArray.Parse(res.GetValue("highlights").ToString());
            string format = res.GetValue("format").ToString();
            string latmatchid = $"https://www.hltv.org/matches/{res.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-')}-vs-" +
                $"{team2.GetValue("name").ToString().Replace(' ', '-')}-{eventInfo.GetValue("name").ToString().Replace(' ', '-')}";
            string mapsString;
            string score0;
            string score1;
            string score2;
            string score3;
            string score4;

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"{team1.GetValue("name")} vs. {team2.GetValue("name")}")
                .WithColor(Color.Red)
                .AddField("event:", $"[{eventInfo.GetValue("name")}]({eventLink})\n{additionalInfo}")
                .AddField("winner:", $"[{winner.GetValue("name")}]({winnerLink})", true)
                .AddField("format:", format, true)
                .WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", latmatchid)
                .WithCurrentTimestamp();
            switch(maps.Count)
            {
                case 1:
                    score0 = JObject.Parse(maps[0].ToString()).GetValue("result").ToString();
                    if(score0.Split(' ').Length > 3)
                    {
                        //Overtime
                        score0 = $"({score0.Split(' ')[0]}) {score0.Split(' ')[1].Replace(";", " |")} {score0.Split(' ')[2].Replace(")", " |")} {score0.Split(' ')[3].Substring(1)}";
                    }
                    else
                    {
                        score0 = $"({score0.Split(' ')[0]}) {score0.Split(' ')[1].Replace(";", " |")} {score0.Split(' ')[2]}";
                    }
                    builder.AddField("maps:", $"{getMapNameByAcronym(JObject.Parse(maps[0].ToString()).GetValue("name").ToString())} {score0}");
                    break;
                case 3:
                    score0 = JObject.Parse(maps[0].ToString()).GetValue("result").ToString();
                    if (score0.Split(' ').Length > 3)
                    {
                        //Overtime
                        score0 = $"({score0.Split(' ')[0]}) {score0.Split(' ')[1].Replace(";", " |")} {score0.Split(' ')[2].Replace(")", " |")} {score0.Split(' ')[3].Substring(1)}";
                    }
                    else
                    {
                        score0 = $"({score0.Split(' ')[0]}) {score0.Split(' ')[1].Replace(";", " |")} {score0.Split(' ')[2]}";
                    }
                    score1 = JObject.Parse(maps[1].ToString()).GetValue("result").ToString();
                    if (score1.Split(' ').Length > 3)
                    {
                        //Overtime
                        score1 = $"({score1.Split(' ')[0]}) {score1.Split(' ')[1].Replace(";", " |")} {score1.Split(' ')[2].Replace(")", " |")} {score1.Split(' ')[3].Substring(1)}";
                    }
                    else
                    {
                        score1 = $"({score1.Split(' ')[0]}) {score1.Split(' ')[1].Replace(";", " |")} {score1.Split(' ')[2]}";
                    }

                    mapsString = $"{getMapNameByAcronym(JObject.Parse(maps[0].ToString()).GetValue("name").ToString())} {score0}\n" +
                        $"{getMapNameByAcronym(JObject.Parse(maps[1].ToString()).GetValue("name").ToString())} {score1}\n";
                    if (JObject.Parse(maps[2].ToString()).GetValue("result").ToString() != "-:- ")
                    {
                        score2 = JObject.Parse(maps[2].ToString()).GetValue("result").ToString();
                        if (score2.Split(' ').Length > 3)
                        {
                            //Overtime
                            score2 = $"({score2.Split(' ')[0]}) {score2.Split(' ')[1].Replace(";", " |")} {score2.Split(' ')[2].Replace(")", " |")} {score2.Split(' ')[3].Substring(1)}";
                        }
                        else
                        {
                            score2 = $"({score2.Split(' ')[0]}) {score2.Split(' ')[1].Replace(";", " |")} {score2.Split(' ')[2]}";
                        }
                        mapsString += $"{getMapNameByAcronym(JObject.Parse(maps[2].ToString()).GetValue("name").ToString())} {score2}";
                    } else
                    {
                        mapsString += $"~~{getMapNameByAcronym(JObject.Parse(maps[2].ToString()).GetValue("name").ToString())}~~";
                    }
                    builder.AddField("maps:", mapsString);
                    break;
                case 5:
                    score0 = JObject.Parse(maps[0].ToString()).GetValue("result").ToString();
                    if (score0.Split(' ').Length > 3)
                    {
                        //Overtime
                        score0 = $"({score0.Split(' ')[0]}) {score0.Split(' ')[1].Replace(";", " |")} {score0.Split(' ')[2].Replace(")", " |")} {score0.Split(' ')[3].Substring(1)}";
                    }
                    else
                    {
                        score0 = $"{score0.Split(' ')[0]} {score0.Split(' ')[1].Replace(";", " |")} {score0.Split(' ')[2]}";
                    }
                    score1 = JObject.Parse(maps[1].ToString()).GetValue("result").ToString();
                    if (score1.Split(' ').Length > 3)
                    {
                        //Overtime
                        score1 = $"({score1.Split(' ')[0]}) {score1.Split(' ')[1].Replace(";", " |")} {score1.Split(' ')[2].Replace(")", " |")} {score1.Split(' ')[3].Substring(1)}";
                    }
                    else
                    {
                        score1 = $"{score1.Split(' ')[0]} {score1.Split(' ')[1].Replace(";", " |")} {score1.Split(' ')[2]}";
                    }
                    score2 = JObject.Parse(maps[2].ToString()).GetValue("result").ToString();
                    if (score2.Split(' ').Length > 3)
                    {
                        //Overtime
                        score2 = $"({score2.Split(' ')[0]}) {score2.Split(' ')[1].Replace(";", " |")} {score2.Split(' ')[2].Replace(")", " |")} {score2.Split(' ')[3].Substring(1)}";
                    }
                    else
                    {
                        score2 = $"({score1.Split(' ')[0]}) {score2.Split(' ')[1].Replace(";", " |")} {score2.Split(' ')[2]}";
                    }


                    mapsString = $"{getMapNameByAcronym(JObject.Parse(maps[0].ToString()).GetValue("name").ToString())} ({score0}\n" +
                        $"{getMapNameByAcronym(JObject.Parse(maps[1].ToString()).GetValue("name").ToString())} ({score1}\n" +
                        $"{getMapNameByAcronym(JObject.Parse(maps[2].ToString()).GetValue("name").ToString())} ({score2}\n";
                    if (JObject.Parse(maps[3].ToString()).GetValue("result").ToString() != "-:- ")
                    {
                        score3 = JObject.Parse(maps[3].ToString()).GetValue("result").ToString();
                        if (score3.Split(' ').Length > 3)
                        {
                            //Overtime
                            score3 = $"({score3.Split(' ')[0]}) {score3.Split(' ')[1].Replace(";", " |")} {score3.Split(' ')[2].Replace(")", " |")} {score3.Split(' ')[3].Substring(1)}";
                        }
                        else
                        {
                            score3 = $"({score3.Split(' ')[0]}) {score3.Split(' ')[1].Replace(";", " |")} {score3.Split(' ')[2]}";
                        }
                        mapsString += $"{getMapNameByAcronym(JObject.Parse(maps[3].ToString()).GetValue("name").ToString())} {score3}";
                    }
                    else
                    {
                        mapsString += $"~~{getMapNameByAcronym(JObject.Parse(maps[3].ToString()).GetValue("name").ToString())}~~";
                    }
                    if (JObject.Parse(maps[4].ToString()).GetValue("result").ToString() != "-:- ")
                    {
                        score4 = JObject.Parse(maps[4].ToString()).GetValue("result").ToString();
                        if (score4.Split(' ').Length > 3)
                        {
                            //Overtime
                            score4 = $"({score4.Split(' ')[0]}) {score4.Split(' ')[1].Replace(";", " |")} {score4.Split(' ')[2].Replace(")", " |")} {score4.Split(' ')[3].Substring(1)}";
                        }
                        else
                        {
                            score4 = $"({score4.Split(' ')[0]}) {score4.Split(' ')[1].Replace(";", " |")} {score4.Split(' ')[2]}";
                        }
                        mapsString += $"{getMapNameByAcronym(JObject.Parse(maps[4].ToString()).GetValue("name").ToString())} {score4}";
                    }
                    else
                    {
                        mapsString += $"~~{getMapNameByAcronym(JObject.Parse(maps[4].ToString()).GetValue("name").ToString())}~~";
                    }
                    builder.AddField("maps:", mapsString);
                    break;
            }
            string title0;
            string title1;
            switch(highlights.Count)
            {
                case 0:
                    break;
                case 1:
                    title0 = JObject.Parse(highlights[0].ToString()).GetValue("title").ToString().Split(" | ")[1];
                    if (title0.Length > 35) { title0 = $"{title0.Substring(0, 35)}\n{title0.Substring(35)}"; }
                    builder.AddField("highlights:", $"[{title0}]({JObject.Parse(highlights[0].ToString()).GetValue("link")})");
                    break;
                case 2:
                    title0 = JObject.Parse(highlights[0].ToString()).GetValue("title").ToString().Split(" | ")[1];
                    if (title0.Length > 35) { title0 = $"{title0.Substring(0, 35)}\n{title0.Substring(35)}"; }
                    title1 = JObject.Parse(highlights[1].ToString()).GetValue("title").ToString().Split(" | ")[1];
                    if (title1.Length > 35) { title1 = $"{title1.Substring(0, 35)}\n{title1.Substring(35)}"; }
                    builder.AddField("highlights:", $"[{title0}]({JObject.Parse(highlights[0].ToString()).GetValue("link")})\n" +
                        $"[{title1}]({JObject.Parse(highlights[1].ToString()).GetValue("link")})");
                    break;
                default:
                    title0 = JObject.Parse(highlights[0].ToString()).GetValue("title").ToString().Split(" | ")[1];
                    if (title0.Length > 35) { title0 = $"{title0.Substring(0, 35)}\n{title0.Substring(35)}"; }
                    title1 = JObject.Parse(highlights[1].ToString()).GetValue("title").ToString().Split(" | ")[1];
                    if (title1.Length > 35) { title1 = $"{title1.Substring(0, 35)}\n{title1.Substring(35)}"; }
                    builder.AddField("highlights:", $"[{title0}]({JObject.Parse(highlights[0].ToString()).GetValue("link")})\n" +
                        $"[{title1}]({JObject.Parse(highlights[1].ToString()).GetValue("link")})\nand {highlights.Count - 2} more");
                    break;
            }

            return builder.Build();
        }
        /// <summary>
        /// Send the latest match as Embed in a Discord SocketTextChannel
        /// </summary>
        /// <param name="channel">List of all Channels in which the embed should be sent</param>
        public async Task AktHLTV(List<SocketTextChannel> channels, DiscordSocketClient client)
        {
            //JObject res = await GetResults();
            (JObject, ushort) res = (await getLatestMatch());
            if (res.Item1 != null)
            {
                Embed embed = GetStats(res.Item1);
                foreach(SocketTextChannel channel in channels)
                {
                    if (res.Item2 >= _cfg.GetServerConfig(channel).MinimumStars)
                    {
//#if RELEASE
                        try { RestUserMessage msg = await channel.SendMessageAsync("", false, embed); await msg.AddReactionAsync(await _cfg.GetEmote(client)); }
                        catch (Discord.Net.HttpException)
                        {
                            Console.WriteLine($"not enough permission in channel {channel}");
                        }
//#endif
                    }
                }
            }
        }


        /// <summary>
        /// Gets stats of a match with given matchlink
        /// </summary>
        /// <param name="matchlink">link of the match</param>
        /// <returns>MatchStats</returns>
        private async Task<(JObject, uint)> GetPLMessage(string matchid)
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/match/" + matchid);
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpResPLStats = await httpResponse.Content.ReadAsStringAsync();
            JObject match = JObject.Parse(httpResPLStats);

            var URI1 = new Uri("https://hltv-api-steel.vercel.app/api/matchstats/" + match.GetValue("statsId"));
            HttpClient http1 = new HttpClient();
            http1.BaseAddress = URI1;
            HttpResponseMessage httpResponse1 = await http1.GetAsync(URI1);
            string matchStats = await httpResponse1.Content.ReadAsStringAsync();
            return (JObject.Parse(matchStats), uint.Parse(match.GetValue("statsId").ToString()));
        }

        /// <summary>
        /// Converts a JArray of a MatchStats into a discord Embed
        /// </summary>
        /// <param name="matchlink"></param>
        /// <returns>Discord Embed</returns>
        private async Task<Embed> GetPLStats(string matchlink)
        {
            EmbedBuilder builder = new EmbedBuilder();

            (JObject, uint) res = await GetPLMessage(matchlink.Substring(29, 7));

            JObject matchStats = res.Item1;
            JObject team1 = JObject.Parse(matchStats.GetValue("team1").ToString());
            JObject team2 = JObject.Parse(matchStats.GetValue("team2").ToString());
            JArray team1PL = JArray.Parse(JObject.Parse(matchStats.GetValue("playerStats").ToString()).GetValue("team1").ToString());
            JArray team2PL = JArray.Parse(JObject.Parse(matchStats.GetValue("playerStats").ToString()).GetValue("team2").ToString());
            var PL0 = JObject.Parse(team1PL[0].ToString());
            var PL1 = JObject.Parse(team1PL[1].ToString());
            var PL2 = JObject.Parse(team1PL[2].ToString());
            var PL3 = JObject.Parse(team1PL[3].ToString());
            var PL4 = JObject.Parse(team1PL[4].ToString());
            var PL5 = JObject.Parse(team2PL[0].ToString());
            var PL6 = JObject.Parse(team2PL[1].ToString());
            var PL7 = JObject.Parse(team2PL[2].ToString());
            var PL8 = JObject.Parse(team2PL[3].ToString());
            var PL9 = JObject.Parse(team2PL[4].ToString());
            string statsLink = $"https://www.hltv.org/stats/matches/{res.Item2}/{team1.GetValue("name").ToString().Replace(' ', '-').ToLower()}-vs-{team2.GetValue("name").ToString().Replace(' ', '-').ToLower()}";


            builder.WithTitle($"PLAYERSTATS ({team1.GetValue("name")} vs. {team2.GetValue("name")})")
                .WithColor(Color.Red)
                .AddField($"players ({team1.GetValue("name")}):", $"{PL0.GetValue("name")}\n{PL1.GetValue("name")}\n{PL2.GetValue("name")}\n{PL3.GetValue("name")}\n{PL4.GetValue("name")}\n", true)
                .AddField("K/A/D:", $"{PL0.GetValue("kills")}/{PL0.GetValue("assists")}/{PL0.GetValue("deaths")}\n" +
                $"{PL1.GetValue("kills")}/{PL1.GetValue("assists")}/{PL1.GetValue("deaths")}\n" +
                $"{PL2.GetValue("kills")}/{PL2.GetValue("assists")}/{PL2.GetValue("deaths")}\n" +
                $"{PL3.GetValue("kills")}/{PL3.GetValue("assists")}/{PL3.GetValue("deaths")}\n" +
                $"{PL4.GetValue("kills")}/{PL4.GetValue("assists")}/{PL4.GetValue("deaths")}", true)
                .AddField("rating:",$"{PL0.GetValue("rating")}\n{PL1.GetValue("rating")}\n{PL2.GetValue("rating")}\n{PL3.GetValue("rating")}\n{PL4.GetValue("rating")}\n", true)
                .AddField($"players ({team2.GetValue("name")}):", $"{PL5.GetValue("name")}\n{PL6.GetValue("name")}\n{PL7.GetValue("name")}\n{PL8.GetValue("name")}\n{PL9.GetValue("name")}", true)
                .AddField("K/A/D", $"{PL5.GetValue("kills")}/{PL5.GetValue("assists")}/{PL5.GetValue("deaths")}\n" +
                $"{PL6.GetValue("kills")}/{PL6.GetValue("assists")}/{PL6.GetValue("deaths")}\n" +
                $"{PL7.GetValue("kills")}/{PL7.GetValue("assists")}/{PL7.GetValue("deaths")}\n" +
                $"{PL8.GetValue("kills")}/{PL8.GetValue("assists")}/{PL8.GetValue("deaths")}\n" +
                $"{PL9.GetValue("kills")}/{PL9.GetValue("assists")}/{PL9.GetValue("deaths")}", true)
                .AddField("rating", $"{PL5.GetValue("rating")}\n{PL6.GetValue("rating")}\n{PL7.GetValue("rating")}\n{PL8.GetValue("rating")}\n{PL9.GetValue("rating")}\n", true)
                .WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", statsLink)
                .WithCurrentTimestamp();

            return builder.Build();
        }

        /// <summary>
        /// Triggerd by reaction. Sends the Embed
        /// </summary>
        /// <returns></returns>
        public async Task stats(string matchlink, ITextChannel channel)
        {
            await channel.SendMessageAsync("", false, await GetPLStats(matchlink));
        }


    }
}
