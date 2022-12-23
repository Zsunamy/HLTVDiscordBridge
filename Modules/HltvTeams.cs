using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvTeams : ModuleBase<SocketCommandContext>
    {
        public static async Task SendTeamCard(SocketSlashCommand arg)
        {
            await arg.DeferAsync();
            try 
            {
                var res = await GetTeamCard(arg.Data.Options.First().Value.ToString());
                await arg.DeleteOriginalResponseAsync();
                await arg.Channel.SendFileAsync(res.Item2, embed: res.Item1);
            }
            catch(HltvApiExceptionLegacy e) { await arg.ModifyOriginalResponseAsync(msg => msg.Embed = ErrorHandling.GetErrorEmbed(e)); }
            
            
        }
        public static async Task<(FullTeam, FullTeamStats)> GetFullTeamAndFullTeamStats(string name)
        {
            Directory.CreateDirectory("./cache/teamcards");
            if (!Directory.Exists($"./cache/teamcards/{name.ToLower().Replace(' ', '-')}"))
            {
                List<string> properties = new();
                List<string> values = new();
                properties.Add("name");
                values.Add(name);
                JObject req;
                try
                {
                    req = await Tools.RequestApiJObject("getTeamByName", properties, values);
                }
                catch (HltvApiExceptionLegacy) { throw; }
                

                FullTeam fullTeam = new FullTeam(req);
                FullTeamStats fullTeamStats;
                try
                {
                    fullTeamStats = await HltvFullTeamStats.GetFullTeamStats(fullTeam.Id);
                }
                catch(HltvApiExceptionLegacy) { throw; }

                Directory.CreateDirectory($"./cache/teamcards/{fullTeam.Name.ToLower().Replace(' ', '-')}");

                //Thumbnail
                HttpClient http = new();
                HttpResponseMessage res = await http.GetAsync(new Uri(fullTeam.Logo));
                string thumbPath;
                try { thumbPath = ConvertSVGtoPNG(await res.Content.ReadAsByteArrayAsync(), fullTeam.Name); }
                catch (System.Xml.XmlException)
                {
                    Bitmap.FromStream(await res.Content.ReadAsStreamAsync()).Save($"./cache/teamcards/" +
                        $"{fullTeam.Name.ToLower().Replace(' ', '-')}/{fullTeam.Name.ToLower().Replace(' ', '-')}_logo.png", System.Drawing.Imaging.ImageFormat.Png); thumbPath = $"./cache/teamcards/" +
                        $"{fullTeam.Name.ToLower().Replace(' ', '-')}/{fullTeam.Name.ToLower().Replace(' ', '-')}_logo.png";
                }
                fullTeam.LocalThumbnailPath = thumbPath;
                
                File.WriteAllText($"./cache/teamcards/{fullTeam.Name.ToLower().Replace(' ', '-')}/fullteam.json", fullTeam.ToString());
                File.WriteAllText($"./cache/teamcards/{fullTeam.Name.ToLower().Replace(' ', '-')}/fullteamstats.json", fullTeamStats.ToString());

                return (fullTeam, fullTeamStats);

            }
            else
            {
                FullTeam fullTeam = new (JObject.Parse(File.ReadAllText($"./cache/teamcards/{name.ToLower().Replace(' ', '-')}/fullteam.json")));
                FullTeamStats fullTeamStats = new (JObject.Parse(File.ReadAllText($"./cache/teamcards/{name.ToLower().Replace(' ', '-')}/fullteamstats.json")));
                return (fullTeam, fullTeamStats);
            }
        }
        private static async Task<(Embed, string)> GetTeamCard(string name)
        {
            (FullTeam, FullTeamStats) res;
            try
            {
                res = await GetFullTeamAndFullTeamStats(name);
            }
            catch (HltvApiExceptionLegacy) { throw; }
            
            EmbedBuilder builder = new();
            FullTeam fullTeam = res.Item1;
            FullTeamStats fullTeamStats = res.Item2;
            if (fullTeam == null && fullTeamStats == null)
            {
                builder.WithTitle("error")
                    .WithColor(Discord.Color.Red)
                    .WithDescription($"The team \"{name}\" does not exist!")
                    .WithCurrentTimestamp();
                return (builder.Build(), "");
            }

            builder.WithTitle(fullTeam?.Name);

            //TeamLink
            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", fullTeam?.Link);

            //Thumbnail            
            builder.WithThumbnailUrl($"attachment://{fullTeam.Name.ToLower().Replace(' ', '-')}_logo.png");

            //rank + development
            short development;
            string rankDevString;
            if (fullTeam.RankingDevelopment.Count < 2) { rankDevString = "n.A"; }
            else
            {
                development = (short)(short.Parse(fullTeam.RankingDevelopment[fullTeam.RankingDevelopment.Count - 1].ToString()) - short.Parse(fullTeam.RankingDevelopment[fullTeam.RankingDevelopment.Count - 2].ToString()));
                Emoji emote;
                string rank = "--";
                if (fullTeam.rank != 0) { rank = fullTeam.rank.ToString(); }
                if (development < 0) { emote = new Emoji("⬆️"); rankDevString = $"{rank} ({emote} {Math.Abs(development)})"; }
                else if (development == 0) { rankDevString = $"{rank} (⏺️ 0)"; }
                else { emote = new Emoji("⬇️"); rankDevString = $"{rank} ({emote} {Math.Abs(development)})"; }

            }

            //stats

            builder.AddField("stats:", "Ranking:\nRounds played:\nMaps played:\nWins/Draws/Losses:\nKills/Deaths:", true);
            builder.AddField("\u200b", $"{rankDevString}\n{fullTeamStats.overview.roundsPlayed}\n{fullTeamStats.overview.mapsPlayed}\n" +
                $"{fullTeamStats.overview.wins}/{fullTeamStats.overview.draws}/{fullTeamStats.overview.losses}\n" +
                $"{fullTeamStats.overview.totalKills}/{fullTeamStats.overview.totalDeaths} (K/D: {fullTeamStats.overview.kdRatio})", true);
            builder.AddField("\u200b", "\u200b", true);

            //teammember
            string lineUpString = "";
            if (fullTeam.Players.Count == 0) { lineUpString = "n.A"; }
            else
            {
                foreach (TeamPlayer pl in fullTeam.Players)
                {
                    lineUpString += $"[{pl.name}]({pl.link}) ({pl.type})\n";
                }
            }
            builder.AddField("member:", lineUpString, true);

            //mapstats
            string mapsStatsString = "";
            if (JObject.FromObject(fullTeamStats.mapStats).Count == 0) { mapsStatsString = "n.A"; }
            else
            {
                for(int i = 0; i < 2; i++)
                {
                    var prop = JObject.FromObject(fullTeamStats.mapStats).Properties().ElementAt(i);
                    TeamMapStats map = new(prop.Value as JObject);
                    mapsStatsString += $"\n**{GetMapNameByAcronym(prop.Name)}** ({map.winRate}% winrate):\n{map.wins} wins, {map.losses} losses\n\n";
                }
            }

            builder.AddField("most played maps:", mapsStatsString, true);
            builder.AddField("\u200b", "\u200b", true);

            //recentResults
            List<Result> recentResults = await HltvResults.GetMatchResults(fullTeam.Id);
            string recentResultsString = "";
            if (recentResults.Count == 0)
            {
                recentResultsString = "n.A";
            }
            else
            {
                foreach (Shared.Result matchResult in recentResults)
                {
                    string opponentTeam;
                    if (matchResult.Team1.name == fullTeam.Name)
                    { opponentTeam = matchResult.Team2.name; }
                    else { opponentTeam = matchResult.Team1.name; }
                    recentResultsString += $"[vs. {opponentTeam}]({matchResult.Link})\n";

                    if(recentResults.IndexOf(matchResult) == 3) { break; }
                }            
            }

            builder.AddField("recent results:", recentResultsString, true);

            //upcoming matches
            /*string upcomingMatchesString = "";
            JArray upcomingMatches = HltvUpcomingAndLiveMatches.SearchUpcoming(teamJObj.GetValue("name").ToString());
            if (upcomingMatches.Count == 0) { upcomingMatchesString = "no upcoming matches"; }
            else
            {
                int j = 0;
                foreach (JObject jObj in upcomingMatches)
                {
                    if (j == 4) { break; }
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
            builder.AddField("upcoming matches:", upcomingMatchesString, true);*/
            builder.AddField("\u200b", "\u200b", true);

            builder.WithCurrentTimestamp();
            builder.WithFooter("The stats shown were collected during the last 3 months");

            /*bool tracked = false;
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
            StatsUpdater.UpdateStats();*/

            return (builder.Build(), fullTeam.LocalThumbnailPath);
        }
        private static string ConvertSVGtoPNG(byte[] svgFile, string teamname)
        {
            string resString = $"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/{teamname.ToLower().Replace(' ', '-')}_logo.png";
            File.WriteAllBytes($"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/logo.svg", svgFile);
            SvgDocument svgDocument;

            try { svgDocument = Svg.SvgDocument.Open($"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/logo.svg"); }
            catch (System.Xml.XmlException) { File.Delete($"./cache/teamcards/{teamname.ToLower().Replace(' ', '-')}/logo.svg"); throw new System.Xml.XmlException(); }

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
    }
}
