using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HLTVDiscordBridge.Modules;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class ApiRequestBody
{
    [JsonIgnore]
    protected static HttpClient Client;

    [JsonIgnore]
    private static readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    public int DelayBetweenRequests = 300;
    protected ApiRequestBody()
    {
        Client ??= new HttpClient();
    }
    public async Task<T> SendRequest<T>(string endpoint)
    {
        Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{endpoint}");
        HttpResponseMessage resp = await Program.GetInstance().DefaultHttpClient.PostAsync(uri, 
            new StringContent(JsonSerializer.Serialize(this, _serializeOptions), Encoding.UTF8, "application/json"));
        switch (resp.StatusCode)
        {
            case HttpStatusCode.OK:
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JsonSerializer.Deserialize<T>(await resp.Content.ReadAsStringAsync());
            case HttpStatusCode.BadRequest:
                throw JsonSerializer.Deserialize<HltvApiException>(await resp.Content.ReadAsStringAsync(), _serializeOptions)!;
            default:
                throw new DeploymentException(resp);
        }
    }
}