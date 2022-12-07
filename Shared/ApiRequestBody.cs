using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public abstract class ApiRequestBody
{
    [JsonIgnore]
    protected static HttpClient Client;
    protected abstract string ToJson();

    protected ApiRequestBody()
    {
        Client ??= new HttpClient();
    }
    
    public async Task<object> SendRequest<returnType>(string endpoint)
    {
        Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{endpoint}");
        HttpResponseMessage resp = await Program.GetInstance().DefaultHttpClient.PostAsync(uri, 
            new StringContent(JsonSerializer.Serialize(this), Encoding.UTF8, "application/json"));
        return JsonSerializer.Deserialize<returnType>(await resp.Content.ReadAsStringAsync());
    }
}