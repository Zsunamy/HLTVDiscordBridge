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
        PlayerId = player.Id;
        Name = player.Ign;
        Alias = new List<string>();
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
        await arg.DeferAsync();
        string name = arg.Data.Options.First().Value.ToString()!.ToLower();
        FullPlayer player;
        PlayerStats stats;
        Embed embed;
        bool isInDatabase = false;
        List<PlayerDocumentNew> query = (await GetPlayerCollection().FindAsync(
            elem => (elem.Alias != null && elem.Alias.Contains(name)) || elem.Name == name)).ToList();
        if (query.Count != 0)
        {
            // Player is in Database
            isInDatabase = true;
            name = query.First().Name;
        }

        if (Directory.Exists($"{Path}/{name.ToLower()}"))
        {
            // Player is cached
            player =  Tools.ParseFromFile<FullPlayer>($"{Path}/{name}/player.json");
            stats = Tools.ParseFromFile<PlayerStats>($"{Path}/{name}/stats.json");
            embed = player.ToEmbed(stats);
        }
        else
        {
            // Player is not cached
            try
            {
                if (isInDatabase)
                {
                    GetPlayer request = new GetPlayer{Id = query.First().PlayerId};
                    player = await request.SendRequest<FullPlayer>();
                }
                else
                {
                    GetPlayerByName request = new GetPlayerByName{Name = name};
                    player = await request.SendRequest<FullPlayer>();
                    List<PlayerDocumentNew> alias = (await GetPlayerCollection().FindAsync(elem => elem.Name == player.Ign)).ToList();
                    if (alias.Count != 0)
                    {
                        alias.First().Alias.Add(name);
                        UpdateDefinition<PlayerDocumentNew> update = Builders<PlayerDocumentNew>.Update.Set(x => x.Alias, alias.First().Alias);
                        await GetPlayerCollection().UpdateOneAsync(x => x.Id == alias.First().Id, update);
                    }
                    else
                    {
                        await GetPlayerCollection().InsertOneAsync(new PlayerDocumentNew(player));
                    }
                }

                if (Directory.Exists($"{Path}/{player.Ign}"))
                {
                    stats = Tools.ParseFromFile<PlayerStats>($"{Path}/{player.Ign}/stats.json");
                }
                else
                {
                    GetPlayerStats requestStats = new GetPlayerStats{Id = player.Id};
                    stats = await requestStats.SendRequest<PlayerStats>();
                    Directory.CreateDirectory($"{Path}/{player.Ign}");
                    Tools.SaveToFile($"{Path}/{player.Ign}/stats.json", stats);
                }
                Tools.SaveToFile($"{Path}/{player.Ign}/player.json", player);

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
}