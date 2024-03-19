using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HLTVDiscordBridge.Modules;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Repository;

public static class ServerConfigRepository
{
    private static readonly IMongoCollection<ServerConfig> Collection =
        Database.DatabaseObj.GetCollection<ServerConfig>("serverconfig");

    public static async Task Insert(ServerConfig config)
    {
        await Collection.InsertOneAsync(config);
    }

    public static async Task<ServerConfig> GetConfigOrNull(ulong id)
    {
        return await Collection.Find(x => x.GuildId == id).FirstOrDefaultAsync();
    }

    public static async Task Delete(ulong id)
    {
        await Collection.DeleteOneAsync(x => x.GuildId == id);
    }

    public static async Task Update(ServerConfig config)
    {
        await Collection.ReplaceOneAsync(x => x.GuildId == config.GuildId, config);
    }

    public static async Task<List<ServerConfig>> GetAll()
    {
        return await Collection.Find(x => true).ToListAsync();
    }

    public static ServerConfig[] GetByFilter(Expression<Func<ServerConfig, bool>> filter)
    {
        return Collection.Find(filter).ToEnumerable().ToArray();
    }

    public static async Task<bool> Exists(ulong id)
    {
        return await Collection.Find(x => x.GuildId == id).AnyAsync();
    }
}