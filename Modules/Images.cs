using System;
using System.Threading.Tasks;
using HLTVDiscordBridge.Repository;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HLTVDiscordBridge.Modules;

public class HltvImage
{
    public string Id { get; set; }
    public string ImageId { get; set; }
    public byte[] Data { get; set; }
    [BsonId]
    public ObjectId Metadata { get; set; } = new();

    public async Task SaveInRepository()
    {
        await ImageRepository.InsertImage(this);
    }

    public static async Task<HltvImage> GetImage(Uri uri)
    {
        string id = uri.AbsolutePath;
        HltvImage imgOrNull = await ImageRepository.GetImageOrNull(id);
        if (imgOrNull != null)
            return imgOrNull;
        
        //TODO Scraper for images
        throw new NotImplementedException("Insert code for scraping images");
    }
}
