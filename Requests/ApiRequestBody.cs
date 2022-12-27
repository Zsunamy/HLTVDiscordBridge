using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Requests;

public abstract class ApiRequestBody<TChild> where TChild : ApiRequestBody<TChild>
{
    [JsonIgnore]
    private static readonly HttpClient Client = new();
    
    protected abstract string Endpoint { get; }

    [JsonIgnore]
    public static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public int DelayBetweenRequests { get; } = 300;

    public async Task<T> SendRequest<T>()
    {
        Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{Endpoint}");
        HttpResponseMessage resp = await Client.PostAsJsonAsync(uri, (TChild)this, SerializeOptions);
        Console.WriteLine(JsonSerializer.Serialize((TChild)this, SerializeOptions));
        try
        {
            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                throw await resp.Content.ReadFromJsonAsync<ApiError>();
            }
            throw new DeploymentException(resp);
        }
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{Endpoint} was successful");
        StatsUpdater.StatsTracker.ApiRequest =+ 1;
        StatsUpdater.UpdateStats();
        return await resp.Content.ReadFromJsonAsync<T>(SerializeOptions);
    }
}