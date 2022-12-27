using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Modules;

internal class PlayerDocumentNew
{
    public PlayerDocumentNew(FullPlayer player)
    {
        PlayerId = int.Parse(player.Id.ToString());
        Name = player.Ign;
        Alias = null;
        Image = player.Image;
        Nationality = player.Country.Code;
    }

    [BsonId]
    public ObjectId Id { get; set; }
    public int PlayerId { get; set; }
    public string Name { get; set; }
    public List<string> Alias { get; set; }
    public string Nationality { get; set; }
    public string Image { get; set; }
}
public static class HltvPlayer
{
    private const string Path = "./cache/playercards";
    private static IMongoCollection<PlayerDocumentNew> GetPlayerCollection()
    {
        MongoClient dbClient = new(BotConfig.GetBotConfig().DatabaseLink);
        IMongoDatabase db = dbClient.GetDatabase(BotConfig.GetBotConfig().Database);
        return db.GetCollection<PlayerDocumentNew>("players");
    }
        
    public static async Task SendPlayerCard(SocketSlashCommand arg)
    {
        string name = arg.Data.Options.First().Value.ToString()!;
        FullPlayer player;
        FullPlayerStats stats;
        Embed embed;
        bool isInDatabase = false;
        IAsyncCursor<PlayerDocumentNew> query = await GetPlayerCollection().FindAsync(elem => elem.Alias.Contains(name.ToLower()));

        if (query.ToList().Count != 0)
        {
            // Player is in Database
            isInDatabase = true;
            name = query.First().Name;
        }
            
        if (Directory.Exists($"{Path}/{name.ToLower()}"))
        {
            // Player is cached
            player =  Tools.ParseFromFile<FullPlayer>($"{Path}/{name.ToLower()}/player.json");
            stats = Tools.ParseFromFile<FullPlayerStats>($"{Path}/{name.ToLower()}/stats.json");
            embed = player.ToEmbed(stats);
        }
        else
        {
            // Player is not cached
            try
            {
                if (isInDatabase)
                {
                    GetPlayer request = new(query.First().PlayerId);
                    player = await request.SendRequest<FullPlayer>();
                }
                else
                {
                    GetPlayerByName request = new(name);
                    player = await request.SendRequest<FullPlayer>();
                    await GetPlayerCollection().InsertOneAsync(new PlayerDocumentNew(player));
                }
                GetPlayerStats requestStats = new(player.Id);
                stats = await requestStats.SendRequest<FullPlayerStats>();
                    
                Tools.SaveToFile($"{Path}/{name}/player.json", player);
                Tools.SaveToFile($"{Path}/{name}/stats.json", stats);

                embed = player.ToEmbed(stats);
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
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
            
        StatsUpdater.StatsTracker.MessagesSent += 1;
        StatsUpdater.UpdateStats();
    }
        
    /*
    private static async void GetPlayer_new(string playername)
    {
        Embed embed;
        GetPlayerByName request = new(playername);
        try
        {
            FullPlayer player = await request.SendRequest<FullPlayer>();
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
    
    public static async Task<Embed> SendPlayer(string name)
    {
        
        List<string> properties = new(); properties.Add("name");
        List<string> values = new(); values.Add(name);

        if (query1.CountDocuments() == 0 && query2.CountDocuments() == 0)
        {
            //nicht in Datenbank
            try { player = new FullPlayer(await Tools.RequestApiJObject("getPlayerByName", properties, values)); }
            catch (HltvApiExceptionLegacy) { throw; }

            collection.InsertOne(new PlayerDocumentNew(player));
        }
        else if (query2.CountDocuments() != 0)
        {
            //unter Alias in Datenbank
            name = query2.FirstOrDefault().Name;
            values.Clear(); values.Add(name);
            if (Directory.Exists($"./cache/playercards/{name.ToLower()}"))
            {
                //Ist unter "richtigem Namen" doch gecached
                player = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{name.ToLower()}/id.json")));
                fullPlayerStats = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{name.ToLower()}/stats.json")));
                return (player, fullPlayerStats);
            }

            try { player = new FullPlayer(await Tools.RequestApiJObject("getPlayerByName", properties, values)); }
            catch (HltvApiExceptionLegacy) { throw; }
        }
        else
        {
            //in Datenbank aber nicht lokal
            try { player = new FullPlayer(await Tools.RequestApiJObject("getPlayerByName", properties, values)); }
            catch(HltvApiExceptionLegacy) { throw; }
        }

        Directory.CreateDirectory($"./cache/playercards/{name.ToLower()}");
        File.WriteAllText($"./cache/playercards/{name.ToLower()}/id.json", JObject.FromObject(player).ToString());
        properties.Clear(); properties.Add("id");
        values.Clear(); values.Add(player.Id.ToString());
        try { fullPlayerStats = new(await Tools.RequestApiJObject("getPlayerStats", properties, values)); }
        catch { throw; }
        File.WriteAllText($"./cache/playercards/{name.ToLower()}/stats.json", JObject.FromObject(fullPlayerStats).ToString());
        return (player, fullPlayerStats);
    }
    
    }
    
    private static async Task<Embed> GetPlayerCard(string playername)
    {
        EmbedBuilder builder = new();
        (FullPlayer, FullPlayerStats) req;
        try { req = await GetPlayerStats_new(playername); }
        catch (Exception) { throw; }

        FullPlayer fullPlayer = req.Item1;
        FullPlayerStats fullPlayerStats = req.Item2;

        if (fullPlayerStats.Image != null) { builder.WithThumbnailUrl(fullPlayerStats.Image); }
        if (fullPlayerStats.Id != 0 && fullPlayerStats.Ign != null) 
        { builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", 
            $"https://hltv.org/player/{fullPlayerStats.Id}/{fullPlayerStats.Ign}"); 
        }
        if(fullPlayerStats.Country.Code != null && fullPlayerStats.Ign != null) { builder.WithTitle(fullPlayerStats.Ign + $" :flag_{fullPlayerStats.Country.Code}:"); }
        if(fullPlayerStats.Name != null) { builder.AddField("Name:", fullPlayerStats.Name, true); }
        if(fullPlayerStats.Age != null) { builder.AddField("Age:", fullPlayerStats.Age, true); } else { builder.AddField("\u200b", "\u200b"); }
        if(fullPlayerStats.Team != null) { builder.AddField("Team:", $"[{fullPlayerStats.Team.Name}]({fullPlayerStats.Team.Link})", true); } else { builder.AddField("Team:", "none"); }
        if (fullPlayerStats.OverviewStatistics != null) 
        {
            builder.AddField("Stats:", "Maps played:\nKills/Deaths:\nHeadshot %:\nADR:\nKills per round:\nAssists per round:\nDeaths per round:", true);
            builder.AddField("\u200b", $"{fullPlayerStats.OverviewStatistics.MapsPlayed}\n{fullPlayerStats.OverviewStatistics.Kills}/{fullPlayerStats.OverviewStatistics.Deaths} " +
                $"({fullPlayerStats.OverviewStatistics.KdRatio})\n{fullPlayerStats.OverviewStatistics.Headshots}\n{fullPlayerStats.OverviewStatistics.DamagePerRound}\n " +
                $"{fullPlayerStats.OverviewStatistics.KillsPerRound}\n {fullPlayerStats.OverviewStatistics.AssistsPerRound}\n {fullPlayerStats.OverviewStatistics.DeathsPerRound}", true);
        }
        builder.WithCurrentTimestamp();

        if(fullPlayer.Achievements.Count != 0)
        {
            List<string> achievements = new();
            foreach(Achievement achievement in fullPlayer.Achievements)
            {             
                if(string.Join("\n", achievements).Length > 600) { break; }
                achievements.Add($"[{achievement.EventObj.Name}]({achievement.EventObj.Link}) finished: {achievement.Place}");
            }
            builder.AddField("Achievements:", string.Join("\n", achievements));

        } else
        {
            builder.AddField("Achievements:", $"none");
        }
        builder.WithFooter(Tools.GetRandomFooter());

        bool tracked = false;
        foreach (PlayerReq plReq in StatsUpdater.StatsTracker.Players)
        {
            if (plReq.Name == fullPlayerStats.Ign)
            {
                StatsUpdater.StatsTracker.Players.Remove(plReq);
                plReq.Reqs += 1;
                StatsUpdater.StatsTracker.Players.Add(new PlayerReq(fullPlayerStats.Ign, (int)fullPlayerStats.Id, plReq.Reqs));
                tracked = true;
                break;
            }
        }
        if (!tracked)
        {
            StatsUpdater.StatsTracker.Players.Add(new PlayerReq(fullPlayerStats.Ign, (int)fullPlayerStats.Id, 1));
        }
        StatsUpdater.UpdateStats();
        return builder.Build();
    }  
    */
}