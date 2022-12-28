using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HLTVDiscordBridge.Modules;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Requests;

public abstract class ApiRequestBody<TChild> where TChild : ApiRequestBody<TChild>
{
    protected abstract string Endpoint { get; }
    public int DelayBetweenRequests { get; } = 300;
    
    public async Task<T> SendRequest<T>()
    {
        Uri uri = new($"{BotConfig.GetBotConfig().ApiLink}/api/{Endpoint}");
        HttpResponseMessage resp = await Program.DefaultHttpClient.PostAsJsonAsync(uri, (TChild)this, Program.SerializeOptions);
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
        return await resp.Content.ReadFromJsonAsync<T>(Program.SerializeOptions);
    }
}