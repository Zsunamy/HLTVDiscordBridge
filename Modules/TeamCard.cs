using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Svg;
using System.Drawing;
using System.Drawing.Imaging;

namespace HLTVDiscordBridge.Modules
{
    public class TeamCard : ModuleBase<SocketCommandContext>
    {
        [Command("team")] 
        public async Task SendTeamCard([Remainder]string name = "")
        {
            //var req = await getTeamCard(name);
            //await Context.Channel.SendFileAsync(req.Item2, embed: req.Item1);
        }

        /// <summary>
        /// gets all available stats of a team
        /// </summary>
        /// <param name="name">name of the team</param>
        /// <returns>Teamstats as JObject; FullTeam as JObject; returns false if the API is down and true if the request was successful</returns>
        async Task<(JObject, JObject, bool)> getTeamStats(string name)
        {
            Directory.CreateDirectory("./cache/teamcards");
            if(!Directory.Exists($"./cache/teamcards/{name.ToLower()}"))
            {
                Uri uri = new Uri($"https://hltv-api-steel.vercel.app/api/team/{name}");
                HttpClient http = new HttpClient();
                HttpResponseMessage res = await http.GetAsync(uri);
                JObject fullTeamJObject;
                try { fullTeamJObject = JObject.Parse(await res.Content.ReadAsStringAsync()); }
                catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return (null, null, false); }
                if (fullTeamJObject.ToString() == "{}") { return (null, null, true); }

                Uri uri1 = new Uri($"https://hltv-api-steel.vercel.app/api/teamstats/{fullTeamJObject.GetValue("id")}/false");
                HttpResponseMessage res1 = await http.GetAsync(uri1);
                JObject teamStats = JObject.Parse(await res1.Content.ReadAsStringAsync());

                Directory.CreateDirectory($"./cache/teamcards/{name.ToLower()}");
                File.WriteAllText($"./cache/teamcards/{name.ToLower()}/fullteam.json", fullTeamJObject.ToString());
                File.WriteAllText($"./cache/teamcards/{name.ToLower()}/teamstats.json", teamStats.ToString());
                return (teamStats, fullTeamJObject, true);

            } else
            {
                JObject fullTeamJObject = JObject.Parse(File.ReadAllText($"./cache/teamcards/{name.ToLower()}/fullteam.json"));
                JObject teamStats = JObject.Parse(File.ReadAllText($"./cache/teamcards/{name.ToLower()}/teamstats.json"));
                return (teamStats, fullTeamJObject, true);
            }            
        }

        async Task<(Embed, string)> getTeamCard(string name)
        {
            var res = await getTeamStats(name);
            EmbedBuilder builder = new EmbedBuilder();
            JObject teamJObj = res.Item2;
            JObject teamStatsJObj = res.Item1;
            if (teamJObj == null && teamStatsJObj == null && res.Item3) 
            {
                builder.WithTitle("ERROR")
                    .WithColor(Discord.Color.Red)
                    .WithDescription($"The team \"{name}\" does not exist!")
                    .WithCurrentTimestamp();
                return (builder.Build(), "");
            } else if(!res.Item3){
                builder.WithColor(Discord.Color.Red)
                    .WithTitle($"SYSTEM ERROR")
                    .WithDescription("Our API is down! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues).")
                    .WithCurrentTimestamp();
                return (builder.Build(), "");
            }

            builder.WithTitle(teamJObj.GetValue("name").ToString());            

            //Thumbnail
            HttpClient client = new HttpClient();
            HttpResponseMessage httpRes = await client.GetAsync(new Uri(teamJObj.GetValue("logo").ToString()));
            string thumbPath = ConvertSVGtoPNG(await httpRes.Content.ReadAsByteArrayAsync(), teamJObj.GetValue("name").ToString());
            builder.WithThumbnailUrl($"attachment://{teamJObj.GetValue("name").ToString().ToLower()}_logo.png");

            //lineup
            JArray lineUp = JArray.Parse(teamStatsJObj.GetValue("currentLineup").ToString());
            string lineUpString = "";
            foreach(JToken jTok in lineUp)
            {
                JObject pl = JObject.Parse(jTok.ToString());
                string plLink = $"https://www.hltv.org/player/{pl.GetValue("id")}/{pl.GetValue("name").ToString().Replace(' ', '-')}";
                lineUpString += $"[{pl.GetValue("name")}]({plLink})\n";
            }
            builder.AddField("lineup:", lineUpString);

            //recentResults
            JArray recentResults = JArray.Parse(teamJObj.GetValue("recentResults").ToString());
            string recentResultsString = "";
            int i = 0;
            foreach(JToken jTok in recentResults)
            {
                if(i == 4) { recentResultsString += $"and {recentResults.Count - 4} more"; break; }
                JObject jObj = JObject.Parse(jTok.ToString());
                JObject enemyTeam = JObject.Parse(jObj.GetValue("enemyTeam").ToString());
                string matchLink = $"https://www.hltv.org/matches/{jObj.GetValue("matchID")}/{teamJObj.GetValue("name").ToString().Replace(' ','-').ToLower()}-vs-" +
                    $"{enemyTeam.GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                recentResultsString += $"[{teamJObj.GetValue("name")} vs. {enemyTeam.GetValue("name")}]({matchLink}) ({jObj.GetValue("result")})\n";
                i++;
            }
            builder.AddField("recent results:", recentResultsString);

            return (builder.Build(), thumbPath);
        }

        private string ConvertSVGtoPNG(byte[] svgFile, string teamname)
        {
            string resString = $"./cache/teamcards/{teamname.ToLower()}/{teamname.ToLower()}_logo.png";
            File.WriteAllBytes($"./cache/teamcards/{teamname.ToLower()}/logo.svg", svgFile);
            var svgDocument = Svg.SvgDocument.Open($"./cache/teamcards/{teamname.ToLower()}/logo.svg");
            svgDocument.ShapeRendering = SvgShapeRendering.Auto;
            Bitmap bmp = svgDocument.Draw(256, 256);
            bmp.Save(resString, System.Drawing.Imaging.ImageFormat.Png);
            File.Delete($"./cache/teamcards/{teamname.ToLower()}/logo.svg");
            return resString;
        }
    }
}
