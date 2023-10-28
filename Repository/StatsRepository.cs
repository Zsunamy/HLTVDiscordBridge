using System.Data;
using HLTVDiscordBridge.Modules;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Repository;

public class StatsRepository
{
    private static readonly IMongoCollection<StatsTracker> Collection =
        Database.DatabaseObj.GetCollection<StatsTracker>("stats");

    public static StatsTracker GetStats()
    {
        StatsTracker stats = Collection.Find(_ => true).FirstOrDefault();
        if (stats == null)
            throw new NoNullAllowedException("stats not found");

        return stats;
    }

    public static void Update(StatsTracker stats)
    {
        Collection.FindOneAndReplace(_ => true, stats);
    }
}