using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules
{
    public class PlayerReq
    {
        public PlayerReq(string name, int id, int reqs)
        {
            Name = name;
            PlayerId = id;
            Reqs = reqs;
        }

        public string Name { get; set; }
        public int PlayerId { get; set; }
        public int Reqs { get; set; }
    }
    public class TeamReq
    {
        public TeamReq(string name, int id, int reqs)
        {
            Name = name;
            TeamId = id;
            Reqs = reqs;
        }
        public string Name { get; set; }
        public int TeamId { get; set; }
        public int Reqs { get; set; }
    }
    public class StatsTracker
    {
        public ObjectId Id { get; set; } = new ObjectId("60941203bd1ee1cd03d32943");
        public int Servercount { get; set; } = 0;
        public int ApiRequest { get; set; } = 0;
        public int Commands { get; set; } = 0;
        public int LiveMatches { get; set; } = 0;
        public int OngoingEvents { get; set; } = 0;
        public int MatchesSent { get; set; } = 0;
        public int NewsSent { get; set; } = 0;
        public int MessagesSent { get; set; } = 0;
        public List<PlayerReq> Players { get; set; } = new ();
        public List<TeamReq> Teams { get; set; } = new ();
    }




    public class StatsUpdater
    {
        public static StatsTracker StatsTracker = new();
        private static IMongoCollection<StatsTracker> GetCollection()
        {
            MongoClient dbClient = new(BotConfig.GetBotConfig().DatabaseLink);
            return dbClient.GetDatabase(BotConfig.GetBotConfig().Database).GetCollection<StatsTracker>("stats");
        }
        public static void InitStats()
        {
            IMongoCollection<StatsTracker> collection = GetCollection();
            StatsTracker = collection.Find(x => x.Id == ObjectId.Parse("60941203bd1ee1cd03d32943")).FirstOrDefault();
        }
        public static void UpdateStats()
        {
            IMongoCollection<StatsTracker> collection = GetCollection();
            if(StatsTracker.ApiRequest < 10) { InitStats(); }
            collection.FindOneAndReplace(x => x.Id == ObjectId.Parse("60941203bd1ee1cd03d32943"), StatsTracker);
        } 
    }
}
