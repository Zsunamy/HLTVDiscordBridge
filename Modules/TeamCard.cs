﻿using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Svg;
using System.Drawing;

namespace HLTVDiscordBridge.Modules
{
    public class TeamCard : ModuleBase<SocketCommandContext>
    {
        #region Commands
        [Command("team")] 
        public async Task SendTeamCard([Remainder]string name = "")
        {
            if(!Directory.Exists($"./cache/teamcards/{name.ToLower().Replace(' ', '-')}"))
            {
                EmbedBuilder builder = new();
                builder.WithTitle("Your request is loading!")
                    .WithDescription("This may take up to 30 seconds")
                    .WithCurrentTimestamp();
                var msg = await Context.Channel.SendMessageAsync(embed: builder.Build());
                IDisposable typingState = Context.Channel.EnterTypingState();
                var req = await GetTeamCard(name);
                typingState.Dispose();
                await msg.DeleteAsync();
                if (req.Item2 == "") { await Context.Channel.SendMessageAsync(embed: req.Item1); }
                StatsUpdater.StatsTracker.MessagesSent += 2;
                StatsUpdater.UpdateStats();
                await Context.Channel.SendFileAsync(req.Item2, embed: req.Item1);
            }   
            else
            {
                var req = await GetTeamCard(name);
                if (req.Item2 == "") { await Context.Channel.SendMessageAsync(embed: req.Item1); }
                StatsUpdater.StatsTracker.MessagesSent += 1;
                StatsUpdater.UpdateStats();
                await Context.Channel.SendFileAsync(req.Item2, embed: req.Item1);
            }            
        }
        #endregion

        #region API
        /// <summary>
        /// gets all available stats of a team
        /// </summary>
        /// <param name="name">name of the team</param>
        /// <returns>Teamstats as JObject; FullTeam as JObject; latestResults as JArray; returns false if the API is down and true if the request was successful; 
        /// returns the path of the teamlogo as string</returns>
        static async Task<(JObject, JObject, bool, string)> GetTeamStats(string name)
        {
            Directory.CreateDirectory("./cache/teamcards");
            if(!Directory.Exists($"./cache/teamcards/{name.ToLower().Replace(' ', '-')}"))
            {
                var req = await Tools.RequestApiJObject($"team/{name}");
                if(!req.Item2) { return (null, null, false, ""); }
                JObject fullTeamJObject;
                fullTeamJObject = req.Item1;
                if (fullTeamJObject == null) { return (null, null, true, ""); }

                await Task.Delay(3000);
                req = await Tools.RequestApiJObject($"teamstats/{fullTeamJObject.GetValue("id")}");
                if(!req.Item2) { return (null, null, false, ""); }
                JObject teamStats = req.Item1;
                
                Directory.CreateDirectory($"./cache/teamcards/{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}");
                File.WriteAllText($"./cache/teamcards/{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}/fullteam.json", fullTeamJObject.ToString());
                File.WriteAllText($"./cache/teamcards/{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}/teamstats.json", teamStats.ToString());

                //Thumbnail
                HttpClient http = new();
                HttpResponseMessage res = await http.GetAsync(new Uri(fullTeamJObject.GetValue("logo").ToString()));
                string thumbPath;
                try { thumbPath = ConvertSVGtoPNG(await res.Content.ReadAsByteArrayAsync(), fullTeamJObject.GetValue("name").ToString()); }
                catch (System.Xml.XmlException) { Bitmap.FromStream(await res.Content.ReadAsStreamAsync()).Save($"./cache/teamcards/" +
                    $"{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}/{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}_logo.png", System.Drawing.Imaging.ImageFormat.Png); thumbPath = $"./cache/teamcards/" +
                    $"{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}/{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}_logo.png"; }

                return (teamStats, fullTeamJObject, true, thumbPath);

            } else
            {
                JObject fullTeamJObject = JObject.Parse(File.ReadAllText($"./cache/teamcards/{name.ToLower().Replace(' ', '-')}/fullteam.json"));
                JObject teamStats = JObject.Parse(File.ReadAllText($"./cache/teamcards/{name.ToLower().Replace(' ', '-')}/teamstats.json"));
                string thumbPath = $"./cache/teamcards/{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}/{teamStats.GetValue("name").ToString().ToLower().Replace(' ', '-')}_logo.png";
                return (teamStats, fullTeamJObject, true, thumbPath);
            }            
        }
        #endregion

        #region Embeds
        private static async Task<(Embed, string)> GetTeamCard(string name)
        {
            var res = await GetTeamStats(name);
            EmbedBuilder builder = new();
            JObject teamJObj = res.Item2;
            JObject teamStatsJObj = res.Item1;
            if (teamJObj == null && teamStatsJObj == null && res.Item3) 
            {
                builder.WithTitle("error")
                    .WithColor(Discord.Color.Red)
                    .WithDescription($"The team \"{name}\" does not exist!")
                    .WithCurrentTimestamp();
                return (builder.Build(), "");
            } else if(!res.Item3){
                builder.WithColor(Discord.Color.Red)
                    .WithTitle($"error")
                    .WithDescription("Our API is currently not available! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues). We're sorry for the inconvience")
                    .WithCurrentTimestamp();
                return (builder.Build(), "");
            }

            builder.WithTitle(teamJObj.GetValue("name").ToString());

            //TeamLink
            string teamLink = $"https://www.hltv.org/team/{teamJObj.GetValue("id")}/{teamJObj.GetValue("name").ToString().Replace(' ', '-')}";
            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", teamLink);

            //Thumbnail            
            builder.WithThumbnailUrl($"attachment://{teamJObj.GetValue("name").ToString().ToLower().Replace(' ', '-')}_logo.png");

            //rank + development
            JArray rankDev = JArray.Parse(teamJObj.GetValue("rankingDevelopment").ToString());
            short development;
            string rankDevString;
            if(rankDev.Count < 2) { rankDevString = "n.A"; }
            else {
                development = (short)(short.Parse(rankDev[rankDev.Count - 1].ToString()) - short.Parse(rankDev[rankDev.Count - 2].ToString()));
                Emoji emote;
                string rank = "--";
                if(teamJObj.GetValue("rank") != null) { rank = teamJObj.GetValue("rank").ToString(); }
                if (development < 0) { emote = new Emoji("⬆️"); rankDevString = $"{rank} ({emote} {Math.Abs(development)})"; }
                else if (development == 0) { rankDevString = $"{rank} (⏺️ 0)"; }
                else { emote = new Emoji("⬇️"); rankDevString = $"{rank} ({emote} {Math.Abs(development)})"; }                
                
            }

            //stats
            JObject statsJObject = JObject.Parse(teamStatsJObj.GetValue("overview").ToString());
            ushort wins = 0, draws = 0, losses = 0;
            if (statsJObject.GetValue("wins") != null) { wins = ushort.Parse(statsJObject.GetValue("wins").ToString()); }
            if (statsJObject.GetValue("draws") != null) { draws = ushort.Parse(statsJObject.GetValue("draws").ToString()); }
            if (statsJObject.GetValue("losses") != null) { losses = ushort.Parse(statsJObject.GetValue("losses").ToString()); }

            builder.AddField("stats:", "Ranking:\nRounds played:\nMaps played:\nWins/Draws/Losses:\nKills/Deaths:", true);
            builder.AddField("\u200b", $"{rankDevString}\n{statsJObject.GetValue("roundsPlayed")}\n{statsJObject.GetValue("mapsPlayed")}\n" +
                $"{wins}/{draws}/{losses}\n" +
                $"{statsJObject.GetValue("totalKills")}/{statsJObject.GetValue("totalDeaths")} (K/D: {statsJObject.GetValue("kdRatio")})", true);
            builder.AddField("\u200b", "\u200b", true);

            //teammember
            JArray lineUp = JArray.Parse(teamJObj.GetValue("players").ToString());
            string lineUpString = "";
            if(lineUp.ToString() == "[]") { lineUpString = "n.A"; }
            else 
            {
                foreach (JObject pl in lineUp)
                {                    
                    string plLink = $"https://www.hltv.org/player/{pl.GetValue("id")}/{pl.GetValue("name").ToString().Replace(' ', '-')}";
                    lineUpString += $"[{pl.GetValue("name")}]({plLink}) ({pl.GetValue("type")})\n";
                }
            }            
            builder.AddField("member:", lineUpString, true);

            //mapstats
            JObject mapStats = JObject.Parse(teamStatsJObj.GetValue("mapStats").ToString());
            string mapsStatsString;
            if (mapStats.ToString() == "{}") { mapsStatsString = "n.A"; }
            else
            {
                JEnumerable<JToken> maps = mapStats.Children();
                JObject map0 = null;
                string map0Name = "";
                JObject map1 = null;
                string map1Name = "";
                int h = 0;
                foreach (JProperty jPro in maps)
                {
                    if (h == 0) { map0Name = jPro.Name; }
                    else if (h == 1) { map1Name = jPro.Name; }
                    else { break; }
                    h++;
                }
                h = 0;
                foreach (JToken jTok in maps.Values())
                {
                    if (h == 0) { map0 = JObject.Parse(jTok.ToString()); }
                    else if (h == 1) { map1 = JObject.Parse(jTok.ToString()); }
                    else { break; }
                    h++;
                }
                mapsStatsString = $"\n**{GetMapNameByAcronym(map0Name)}** ({map0.GetValue("winRate")}% winrate):\n{map0.GetValue("wins")} wins, {map0.GetValue("losses")} losses\n\n" +
                    $"**{GetMapNameByAcronym(map1Name)}** ({map1.GetValue("winRate")}% winrate):\n{map1.GetValue("wins")} wins, {map1.GetValue("losses")} losses";
            }            
            
            builder.AddField("most played maps:", mapsStatsString, true);
            builder.AddField("\u200b", "\u200b", true);

            //recentResults
            await Task.Delay(3000);
            JArray recentResults = await HltvResults.GetResults(ushort.Parse(teamStatsJObj.GetValue("id").ToString()));
            string recentResultsString = "";
            if (recentResults == null)
            {
                recentResultsString = "n.A";
            } else
            {
                for (int i = 0; i < recentResults.Count; i++)
                {
                    if (i == 4) { break; }
                    JObject result = JObject.Parse(recentResults[i].ToString());
                    string opponentTeam;
                    JObject team1 = JObject.Parse(result.GetValue("team1").ToString());
                    JObject team2 = JObject.Parse(result.GetValue("team2").ToString());
                    if (team1.GetValue("name").ToString() == teamJObj.GetValue("name").ToString())
                    { opponentTeam = team2.GetValue("name").ToString(); }
                    else { opponentTeam = JObject.Parse(result.GetValue("team1").ToString()).GetValue("name").ToString(); }
                    string link = $"https://www.hltv.org/matches/{result.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-')}-vs-" +
                    $"{team2.GetValue("name").ToString().Replace(' ', '-')}";
                    recentResultsString += $"[vs. {opponentTeam}]({link})\n";
                }
            }
            
            builder.AddField("recent results:", recentResultsString, true);

            //upcoming matches
            string upcomingMatchesString = "";
            JArray upcomingMatches = HltvUpcomingAndLiveMatches.SearchUpcoming(teamJObj.GetValue("name").ToString());
            if(upcomingMatches.Count == 0) { upcomingMatchesString = "no upcoming matches"; }
            else
            {
                int j = 0;
                foreach(JObject jObj in upcomingMatches)
                {
                    if(j == 4) { break; }
                    string opponentTeam;
                    JObject team1 = JObject.Parse(jObj.GetValue("team1").ToString());
                    JObject team2 = JObject.Parse(jObj.GetValue("team2").ToString());
                    if (team1.GetValue("name").ToString() == teamJObj.GetValue("name").ToString())
                    { opponentTeam = team2.GetValue("name").ToString(); }
                    else { opponentTeam = JObject.Parse(jObj.GetValue("team1").ToString()).GetValue("name").ToString(); }
                    string matchLink = $"https://www.hltv.org/matches/{jObj.GetValue("matchID")}/{team1.GetValue("name").ToString().Replace(' ', '-').ToLower()}-vs-" +
                    $"{team2.GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    upcomingMatchesString += $"[vs. {opponentTeam}]({matchLink})\n";
                    j++;
                }                
            }
            builder.AddField("upcoming matches:", upcomingMatchesString, true);
            builder.AddField("\u200b", "\u200b", true);

            builder.WithCurrentTimestamp();
            builder.WithFooter("The stats shown were collected during the last 3 months");

            bool tracked = false;
            foreach (TeamReq teamReq in StatsUpdater.StatsTracker.Teams)
            {
                if (teamReq.Name == teamJObj.GetValue("name").ToString())
                {
                    StatsUpdater.StatsTracker.Teams.Remove(teamReq);
                    teamReq.Reqs += 1;
                    StatsUpdater.StatsTracker.Teams.Add(new TeamReq(teamJObj.GetValue("name").ToString(), int.Parse(teamJObj.GetValue("id").ToString()), teamReq.Reqs));
                    tracked = true;
                    break;
                }
            }
            if (!tracked)
            {
                StatsUpdater.StatsTracker.Teams.Add(new TeamReq(teamJObj.GetValue("name").ToString(), int.Parse(teamJObj.GetValue("id").ToString()), 1));
            }
            StatsUpdater.UpdateStats();

            return (builder.Build(), res.Item4);
        }
        #endregion

        #region tools
        /// <summary>
        /// Converts a SVGImage to an Image
        /// </summary>
        /// <param name="svgFile">svgFile as byte array</param>
        /// <param name="teamname">teamname of the logo</param>
        /// <returns>path of saved image</returns>
        private static string ConvertSVGtoPNG(byte[] svgFile, string teamname)
        {
            string resString = $"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/{teamname.ToLower().Replace(' ','-')}_logo.png";
            File.WriteAllBytes($"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/logo.svg", svgFile);
            SvgDocument svgDocument;

            try { svgDocument = Svg.SvgDocument.Open($"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/logo.svg"); }
            catch(System.Xml.XmlException) { File.Delete($"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/logo.svg"); throw new System.Xml.XmlException(); }

            svgDocument.ShapeRendering = SvgShapeRendering.Auto;
            Bitmap bmp = svgDocument.Draw(256, 256);
            File.Delete($"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/logo.svg");
            bmp.Save(resString, System.Drawing.Imaging.ImageFormat.Png);            
            return resString;
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
        #endregion
    }
}
