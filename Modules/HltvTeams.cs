using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;


namespace HLTVDiscordBridge.Modules;

internal class TeamDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public int TeamId { get; set; }
    public string Name { get; set; }
    public List<string> Alias { get; set; }
    public string Nationality { get; set; }
    public string Logo { get; set; }

    public TeamDocument(FullTeam team)
    {
        TeamId = team.Id;
        Name = team.Name;
        Alias = new List<string>();
        Nationality = team.Country.Code;
        Logo = team.Logo;
    }
}

public static class HltvTeams
{
    private const string Path = "./cache/teamcards";
    private static IMongoCollection<TeamDocument> GetTeamCollection()
    {
        IMongoDatabase db = Program.DbClient.GetDatabase(BotConfig.GetBotConfig().Database);
        return db.GetCollection<TeamDocument>("teams");
    }
    
    public static async Task<int?> GetIdFromDatabase(string name)
    {
        IFindFluent<TeamDocument, TeamDocument> query = GetTeamCollection().Find(
            elem => elem.Alias.Contains(name) || elem.Name.ToLower() == name);
        if (await query.AnyAsync())
        {
            return query.First().TeamId;
        }
        return null;
    }
    
    public static async Task SendTeamCard(SocketSlashCommand arg)
    {
        string name = arg.Data.Options.First().Value.ToString()!.ToLower();
        HttpResponseMessage resp = null;
        FullTeam team = null;
        Embed embed;
        bool isInDatabase = false;
        string name1 = name;
        FindFluentBase<TeamDocument, TeamDocument> query = (FindFluentBase<TeamDocument, TeamDocument>)GetTeamCollection().Find(
            elem => elem.Alias.Contains(name1) || elem.Name.ToLower() == name1);
        if (await query.AnyAsync())
        {
            // Team is in Database
            isInDatabase = true;
            name = query.First().Name.ToLower();
        }
        try
        {
            string startDate = Tools.GetHltvTimeFormat(DateTime.Now.AddMonths(-3));
            string endDate = Tools.GetHltvTimeFormat(DateTime.Now);
            GetResults resultsRequest = new GetResults { StartDate = startDate, EndDate = endDate };

            Result[] recentResults;
            FullTeamStats stats;
            if (Directory.Exists($"{Path}/{name}/"))
            {
                // Team is cached
                team =  Tools.ParseFromFile<FullTeam>($"{Path}/{name}/team.json");
                stats = Tools.ParseFromFile<FullTeamStats>($"{Path}/{name}/stats.json");
                resultsRequest.TeamIds = [team.Id];
                recentResults = await resultsRequest.SendRequest<Result[]>();
                embed = team.ToEmbed(stats, recentResults);
            }
            else
            {
                if (isInDatabase)
                {
                    GetTeam request = new GetTeam { Id = query.First().TeamId };
                    team = await request.SendRequest<FullTeam>();
                }
                else
                {
                    GetTeamByName request = new GetTeamByName { Name = name };
                    team = await request.SendRequest<FullTeam>();
                    List<TeamDocument> alias =
                        (await GetTeamCollection().FindAsync(elem => elem.Name == team.Name)).ToList();
                    // check if provided name is another nickname for the team and add them to the alias
                    if (alias.Count != 0 && !alias.First().Name.ToLower().Contains(name))
                    {
                        alias.First().Alias.Add(name);
                        UpdateDefinition<TeamDocument> update =
                            Builders<TeamDocument>.Update.Set(x => x.Alias, alias.First().Alias);
                        await GetTeamCollection().UpdateOneAsync(x => x.Id == alias.First().Id, update);
                    }
                    else if (alias.Count == 0)
                    {
                        await GetTeamCollection().InsertOneAsync(new TeamDocument(team));
                    }
                }

                // check again if player is cached because provided name could have been an unknown nickname
                if (Directory.Exists($"{Path}/{team.FormattedName}"))
                {
                    stats = Tools.ParseFromFile<FullTeamStats>($"{Path}/{team.FormattedName}/stats.json");
                    resultsRequest.TeamIds = new[] { team.Id };
                    recentResults = await resultsRequest.SendRequest<Result[]>();
                }
                else
                {
                    GetTeamStats statsRequest = new GetTeamStats { Id = team.Id };
                    resultsRequest.TeamIds = new[] { team.Id };
                    stats = await statsRequest.SendRequest<FullTeamStats>();
                    recentResults = await resultsRequest.SendRequest<Result[]>();

                    Directory.CreateDirectory($"{Path}/{team.FormattedName}");
                    Tools.SaveToFile($"{Path}/{team.FormattedName}/stats.json", stats);
                }

                resp = await Program.DefaultHttpClient.GetAsync(new Uri(team.Logo));
                if (!resp.IsSuccessStatusCode)
                {
                    File.Copy("../teamplaceholder.png", $"{Path}/{team.FormattedName}/logo.png", true);
                }
                else
                {
                    SavePng(await resp.Content.ReadAsByteArrayAsync(), team, $"{Path}/{team.FormattedName}/logo.png");
                }
                
                Tools.SaveToFile($"{Path}/{team.FormattedName}/team.json", team);

                embed = team.ToEmbed(stats, recentResults);
            }
        }
        
        catch (ApiError ex)
        {
            embed = ex.ToEmbed();
        }
        catch (DeploymentException ex)
        {
            embed = ex.ToEmbed();
        }
        catch (HttpRequestException)
        {
            embed = new DeploymentException(resp).ToEmbed();
        }

        await arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            if (team != null)
            {
                msg.Attachments = new FileAttachment[] {new ($"{Path}/{team.FormattedName}/logo.png")};
            }
        });
    }
    
    private static void SavePng(byte[] data, FullTeam team, string path)
    {
        try
        {
            File.WriteAllBytes($"{Path}/{team.FormattedName}/logo.svg", data);
            SvgDocument svgDocument;

            try
            {
                svgDocument = SvgDocument.Open($"{Path}/{team.FormattedName}/logo.svg");
            }
            catch (System.Xml.XmlException)
            {
                File.Delete($"{Path}/{team.FormattedName}/logo.svg");
                throw;
            }

            svgDocument.ShapeRendering = SvgShapeRendering.Auto;
            Bitmap bmp = svgDocument.Draw(256, 256);
            File.Delete($"{Path}/{team.FormattedName}/logo.svg");
            bmp.Save(path, ImageFormat.Png);
        }
        catch (System.Xml.XmlException)
        {
            using Image image = Image.FromStream(new MemoryStream(data));
            image.Save(path, ImageFormat.Png);
        }
    }
}