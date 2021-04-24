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
    public class StatsStruct
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public int ApiRequest { get; set; }
    }
    public class Stats
    {
        private static IMongoCollection<StatsStruct> GetCollection()
        {
            MongoClient dbClient = new(Config.LoadConfig().DatabaseLink);
#if RELEASE
            IMongoDatabase db = dbClient.GetDatabase("hltv");
#endif
#if DEBUG
            IMongoDatabase db = dbClient.GetDatabase("hltv-dev");
#endif
            return db.GetCollection<StatsStruct>("stats");
        }
        public static void AddApiRequests(ushort count)
        {
            IMongoCollection<StatsStruct> collection = GetCollection();
        }
    }
}
