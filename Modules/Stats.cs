using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public List<PlayerReq> Players { get; set; } = new List<PlayerReq>();
        public List<TeamReq> Teams { get; set; } = new List<TeamReq>();
    }




    public class StatsUpdater
    {
        public static StatsTracker StatsTracker = new();
        private static IMongoCollection<StatsTracker> GetCollection()
        {
            MongoClient dbClient = new(Config.LoadConfig().DatabaseLink);
#if RELEASE
            IMongoDatabase db = dbClient.GetDatabase("hltv");
#endif
#if DEBUG
            IMongoDatabase db = dbClient.GetDatabase("hltv-dev");
#endif
            return db.GetCollection<StatsTracker>("stats");
        }
        public static void UpdateStats()
        {
            IMongoCollection<StatsTracker> collection = GetCollection();
            collection.FindOneAndReplace(x => x.Id == ObjectId.Parse("60941203bd1ee1cd03d32943"), StatsTracker);
        } 
    }
}
