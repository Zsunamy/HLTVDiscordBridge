using System.Threading.Tasks;
using HLTVDiscordBridge.Modules;
using MongoDB.Driver;

namespace HLTVDiscordBridge.Repository;

public static class ImageRepository
{
    private static readonly IMongoCollection<HltvImage> Collection =
        Database.DatabaseObj.GetCollection<HltvImage>("images");

    public static async Task<HltvImage> GetImageOrNull(string id)
    {
        return await Collection.Find(x => x.ImageId == id).FirstOrDefaultAsync();
    }

    public static async Task<bool> ExistsImage(string id)
    {
        return await GetImageOrNull(id) != null;
    }

    public static async Task InsertImage(HltvImage img)
    {
        await Collection.InsertOneAsync(img);
    }
}