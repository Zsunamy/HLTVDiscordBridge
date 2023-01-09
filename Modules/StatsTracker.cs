﻿using System.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using HLTVDiscordBridge.Shared;
using MongoDB.Bson.Serialization.Attributes;

namespace HLTVDiscordBridge.Modules;

public class StatsTracker
{
    [BsonIgnore] private static StatsTracker Instance { get; set; }
    public ObjectId Id { get; set; }
    public int ServerCount { get; set; }
    public int ApiRequest { get; set; }
    public int Commands { get; set; } = 0;
    public int LiveMatches { get; set; } = 0;
    public int OngoingEvents { get; set; } = 0;
    public int MatchesSent { get; set; } = 0;
    public int NewsSent { get; set; } = 0;
    public int MessagesSent { get; set; }
    
    private StatsTracker() {}
    
    private static IMongoCollection<StatsTracker> GetStatsCollection()
    {
        return Program.DbClient.GetDatabase(BotConfig.GetBotConfig().Database).GetCollection<StatsTracker>("stats");
    }
    public static StatsTracker GetStats()
    {
        Instance ??= GetStatsCollection().FindSync(x => x.Id == ObjectId.Parse("60941203bd1ee1cd03d32943")).FirstOrDefault();
        if (Instance == null)
        {
            throw new NoNullAllowedException("Stats not found");
        }

        return Instance;
    }
    public void Update()
    {
        GetStatsCollection().FindOneAndReplace((x => x.Id == Id), this);
    }
}