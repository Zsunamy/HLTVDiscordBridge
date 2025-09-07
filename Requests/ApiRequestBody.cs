using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using Discord;
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
        
        // Use memory-efficient JSON serialization
        using var content = JsonContent.Create((TChild)this, options: Program.SerializeOptions);
        using HttpResponseMessage resp = await Program.DefaultHttpClient.PostAsync(uri, content);
        
        try
        {
            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                throw await resp.Content.ReadFromJsonAsync<ApiError>(Program.SerializeOptions);
            }
            throw new DeploymentException(resp);
        }

        Logger.Log(new MyLogMessage(LogSeverity.Verbose, ((TChild)this).GetType().Name, "was successful"));
        StatsTracker.GetStats().ApiRequest += 1;
        
        // Use streaming deserialization to reduce memory pressure
        await using var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, Program.SerializeOptions);
    }
}