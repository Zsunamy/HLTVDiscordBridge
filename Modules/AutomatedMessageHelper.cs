using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class AutomatedMessageHelper
{
    public static List<T> ParseFromFile<T>(string path)
    {
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), ApiRequestBody.SerializeOptions);
    }
    public static async Task<bool> VerifyFile<T>(string path, Func<Task<T>> getNewData)
    {
        if (File.Exists(path))
        {
            try
            {
                JsonDocument.Parse(await File.ReadAllTextAsync(path));
                return true;
            }
            catch (JsonException) {}
        }
        SaveToFile(path, await getNewData());
        return false;
    }

    public static void SaveToFile(string path, object content)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(content, ApiRequestBody.SerializeOptions));
    }
}