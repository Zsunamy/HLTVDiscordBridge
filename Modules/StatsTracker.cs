using System.Data;
using HLTVDiscordBridge.Repository;
using MongoDB.Bson;
using MongoDB.Driver;
using HLTVDiscordBridge.Shared;
using MongoDB.Bson.Serialization.Attributes;

namespace HLTVDiscordBridge.Modules;

public class StatsTracker
{
    [BsonIgnore] private static StatsTracker Instance { get; set; }
    [BsonId] public ObjectId Id { get; set; }
    public int ServerCount { get; set; }
    public int ApiRequest { get; set; }
    public int Commands { get; set; } = 0;
    public int LiveMatches { get; set; } = 0;
    public int OngoingEvents { get; set; } = 0;
    public int MatchesSent { get; set; } = 0;
    public int NewsSent { get; set; } = 0;
    public int EventsSent { get; set; } = 0;
    public int ResultsSent { get; set; } = 0;
    public int MessagesSent { get; set; }
    
    private StatsTracker() {}
    
    public static StatsTracker GetStats()
    {
        Instance ??= StatsRepository.GetStats();
        if (Instance == null)
        {
            throw new NoNullAllowedException("Stats not found");
        }

        return Instance;
    }
}