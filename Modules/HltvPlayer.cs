using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.Repository;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Modules;

public class PlayerDocument
{
    public PlayerDocument(FullPlayer player)
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
        
    public static async Task SendPlayerCard(SocketSlashCommand arg)
    {
        string name = arg.Data.Options.First().Value.ToString()!.ToLower();
        FullPlayer player;
        PlayerStats stats;
        Embed embed;
        PlayerDocument query = await PlayerRepository.FindByName(name);
        bool isInDatabase = query != null;
        if (query != null)
        {
            // Player is in Database
            name = query.Name;
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
            // Player under provided name is not cached
            try
            {
                if (isInDatabase)
                {
                    GetPlayer request = new GetPlayer{Id = query.PlayerId};
                    player = await request.SendRequest<FullPlayer>();
                }
                else
                {
                    GetPlayerByName request = new GetPlayerByName{Name = name};
                    player = await request.SendRequest<FullPlayer>();
                    PlayerDocument alias = await PlayerRepository.FindByName(player.Ign);
                    // check if provided name is another nickname for the player and add them to the alias
                    if (alias != null && !alias.Name.ToLower().Contains(name))
                    {
                        alias.Alias.Add(name);
                        await PlayerRepository.UpdateByPlayerId(alias.PlayerId, alias);
                    }
                    else if (alias != null)
                    {
                        await PlayerRepository.Insert(new PlayerDocument(player));
                    }
                }
                // check again if player is cached because provided name could have been an unknown nickname
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
    }
}