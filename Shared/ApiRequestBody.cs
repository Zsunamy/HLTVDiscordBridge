using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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
    public static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    public int DelayBetweenRequests = 300;
    public ApiRequestBody()
    {
        Client ??= new HttpClient();
    }
    public async Task<T> SendRequest<T>(string endpoint)
    {
        Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{endpoint}");
        HttpResponseMessage resp = await Program.GetInstance().DefaultHttpClient.PostAsJsonAsync(uri, this, SerializeOptions);
        try
        {
            resp.EnsureSuccessStatusCode();
            Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
            StatsUpdater.StatsTracker.ApiRequest = +1;
            StatsUpdater.UpdateStats();
            return JsonSerializer.Deserialize<T>(await resp.Content.ReadAsStringAsync(), SerializeOptions);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                throw (await resp.Content.ReadFromJsonAsync<HltvApiException>())!;
                //throw JsonSerializer.Deserialize<HltvApiException>(await resp.Content.ReadAsStringAsync(), SerializeOptions)!;
            }
            throw new DeploymentException(resp);
        }
        /*switch (resp.StatusCode)
        {
            case HttpStatusCode.OK:
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JsonSerializer.Deserialize<T>(await resp.Content.ReadAsStringAsync(), SerializeOptions);
            case HttpStatusCode.BadRequest:
                throw JsonSerializer.Deserialize<HltvApiException>(await resp.Content.ReadAsStringAsync(), SerializeOptions)!;
            default:
                throw new DeploymentException(resp);
        }*/
    }
}