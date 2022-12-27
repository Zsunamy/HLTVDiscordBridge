using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Svg;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;


namespace HLTVDiscordBridge.Modules;

public static class HltvTeams
{
    private const string Path = "./cache/teamcards";
    public static async Task SendTeamCard(SocketSlashCommand arg)
    {
        await arg.DeferAsync();
        string name = arg.Data.Options.First().Value.ToString();
        Embed embed;
        FullTeam? team = null;
        if (!Directory.Exists($"{Path}/{name.ToLower().Replace(' ', '-')}"))
        {
            try
            {
                GetTeamByName request = new(name);
                team = await request.SendRequest<FullTeam>();
                FullTeamStats stats = await new GetTeamStats(team.Id).SendRequest<FullTeamStats>();
                HttpResponseMessage res = await Program.GetInstance().DefaultHttpClient.GetAsync(new Uri(team.Logo));
                
                team.LocalThumbnailPath = await SavePng(await res.Content.ReadAsByteArrayAsync(), team.Name);
                
                embed = await team.ToEmbed(stats);
            }
            catch (ApiError ex)
            {
                embed = ex.ToEmbed();
            }
            catch (DeploymentException ex)
            {
                embed = ex.ToEmbed();
            }
        }
        else
        {
            team = Tools.ParseFromFile<FullTeam>($"{Path}/{name.ToLower().Replace(' ', '-')}/team.json");
            FullTeamStats stats = Tools.ParseFromFile<FullTeamStats>($"{Path}/{name.ToLower().Replace(' ', '-')}/stats.json");
            embed = await team.ToEmbed(stats);
        }

        await arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            if (team is { LocalThumbnailPath: { } })
            {
                msg.Attachments = new[] { new FileAttachment(team.LocalThumbnailPath) };
            }
        });
        /*
        try 
        {
            var res = await GetTeamCard(arg.Data.Options.First().Value.ToString());
            await arg.DeleteOriginalResponseAsync();
            await arg.Channel.SendFileAsync(res.Item2, embed: res.Item1);
        }
        catch(HltvApiExceptionLegacy e) { await arg.ModifyOriginalResponseAsync(msg => msg.Embed = ErrorHandling.GetErrorEmbed(e)); }
        */
    }
    /*
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
            if (fullTeam.Rank != 0) { rank = fullTeam.Rank.ToString(); }
            if (development < 0) { emote = new Emoji("⬆️"); rankDevString = $"{rank} ({emote} {Math.Abs(development)})"; }
            else if (development == 0) { rankDevString = $"{rank} (⏺️ 0)"; }
            else { emote = new Emoji("⬇️"); rankDevString = $"{rank} ({emote} {Math.Abs(development)})"; }

        }

        //stats

        builder.AddField("stats:", "Ranking:\nRounds played:\nMaps played:\nWins/Draws/Losses:\nKills/Deaths:", true);
        builder.AddField("\u200b", $"{rankDevString}\n{fullTeamStats.Overview.RoundsPlayed}\n{fullTeamStats.Overview.MapsPlayed}\n" +
            $"{fullTeamStats.Overview.Wins}/{fullTeamStats.Overview.Draws}/{fullTeamStats.Overview.Losses}\n" +
            $"{fullTeamStats.Overview.TotalKills}/{fullTeamStats.Overview.TotalDeaths} (K/D: {fullTeamStats.Overview.KdRatio})", true);
        builder.AddField("\u200b", "\u200b", true);

        //teammember
        string lineUpString = "";
        if (fullTeam.Players.Count == 0) { lineUpString = "n.A"; }
        else
        {
            foreach (TeamPlayer pl in fullTeam.Players)
            {
                lineUpString += $"[{pl.Name}]({pl.Link}) ({pl.Type})\n";
            }
        }
        builder.AddField("member:", lineUpString, true);

        //mapstats
        string mapsStatsString = "";
        if (JObject.FromObject(fullTeamStats.MapStats).Count == 0) { mapsStatsString = "n.A"; }
        else
        {
            for(int i = 0; i < 2; i++)
            {
                var prop = JObject.FromObject(fullTeamStats.MapStats).Properties().ElementAt(i);
                TeamMapStats map = new(prop.Value as JObject);
                mapsStatsString += $"\n**{Tools.GetMapNameByAcronym(prop.Name)}** ({map.WinRate}% winrate):\n{map.Wins} wins, {map.Losses} losses\n\n";
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
                if (matchResult.Team1.Name == fullTeam.Name)
                { opponentTeam = matchResult.Team2.Name; }
                else { opponentTeam = matchResult.Team1.Name; }
                recentResultsString += $"[vs. {opponentTeam}]({matchResult.Link})\n";

                if(recentResults.IndexOf(matchResult) == 3) { break; }
            }            
        }

        builder.AddField("recent results:", recentResultsString, true);

        //upcoming matches
        string upcomingMatchesString = "";
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
        builder.AddField("upcoming matches:", upcomingMatchesString, true);
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
        StatsUpdater.UpdateStats();

        return (builder.Build(), fullTeam.LocalThumbnailPath);
    }
    */
    private static async Task<string> SavePng(byte[] data, string name)
    {
        string path = $"{Path}/{name.ToLower().Replace(' ', '-')}/{name.ToLower().Replace(' ', '-')}_logo.png";
        try
        {
            /*
            await using (FileStream fileStream = File.Create("{Path}/{name.ToLower().Replace(' ', '-')}/logo.svg"))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream);
            }
            */
            File.WriteAllBytes($"{Path}/{name.ToLower().Replace(' ', '-')}/logo.svg", data);
            SvgDocument svgDocument;

            try
            {
                svgDocument = SvgDocument.Open($"{Path}/{name.ToLower().Replace(' ', '-')}/logo.svg");
            }
            catch (System.Xml.XmlException)
            {
                File.Delete($"{Path}/{name.ToLower().Replace(' ', '-')}/logo.svg");
                throw;
            }

            svgDocument.ShapeRendering = SvgShapeRendering.Auto;
            Bitmap bmp = svgDocument.Draw(256, 256);
            File.Delete($"{Path}/{name.ToLower().Replace(' ', '-')}/logo.svg");
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
        catch (System.Xml.XmlException)
        {
            using(Image image = Image.FromStream(new MemoryStream(data)))
            {
                image.Save($"{Path}/{name.ToLower().Replace(' ', '-')}/{name.ToLower().Replace(' ', '-')}_logo.png",
                    ImageFormat.Png);
            }
        }

        return path;
    }
    /*
    private static string ConvertSvGtoPng(byte[] svgFile, string teamName)
    {
        string resString = $"{Path}/{teamName.ToLower().Replace(' ', '-')}/{teamName.ToLower().Replace(' ', '-')}_logo.png";
        File.WriteAllBytes($"{Path}/{teamName.ToLower().Replace(' ', '-')}/logo.svg", svgFile);
        SvgDocument svgDocument;

        try { svgDocument = Svg.SvgDocument.Open($"./cache/teamcards/{teamName.ToLower().Replace(' ', '-')}/logo.svg"); }
        catch (System.Xml.XmlException) { File.Delete($"./cache/teamcards/{teamName.ToLower().Replace(' ', '-')}/logo.svg"); throw new System.Xml.XmlException(); }

        svgDocument.ShapeRendering = SvgShapeRendering.Auto;
        Bitmap bmp = svgDocument.Draw(256, 256);
        File.Delete($"./cache/teamcards/{teamName.ToLower().Replace(' ', '-')}/logo.svg");
        bmp.Save(resString, System.Drawing.Imaging.ImageFormat.Png);
        return resString;
    }
    */
}