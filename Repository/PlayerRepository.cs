using System.Threading.Tasks;
using HLTVDiscordBridge.Modules;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Repository;

public static class PlayerRepository
{
    private static readonly IMongoCollection<PlayerDocument> Collection =
        Database.DatabaseObj.GetCollection<PlayerDocument>("players");

    public static async Task<PlayerDocument> FindByName(string name)
    {
        return await Collection.Find(x => x.Name.ToLower() == name || x.Alias.Contains(name)).FirstOrDefaultAsync();
    }

    public static async Task UpdateByPlayerId(int id, PlayerDocument player)
    {
        await Collection.FindOneAndReplaceAsync(x => x.PlayerId == id, player);
    }

    public static async Task Insert(PlayerDocument player)
    {
        await Collection.InsertOneAsync(player);
    }
}