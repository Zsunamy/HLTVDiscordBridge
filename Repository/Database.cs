using HLTVDiscordBridge.Shared;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Repository;

public static class Database
{
    private static readonly MongoClient MongoClient = new(BotConfig.GetBotConfig().DatabaseLink);
    public static readonly IMongoDatabase DatabaseObj = MongoClient.GetDatabase(BotConfig.GetBotConfig().Database);
}