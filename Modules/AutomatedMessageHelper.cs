using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules;

public static class AutomatedMessageHelper
{
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
        Tools.SaveToFile(path, await getNewData());
        return false;
    }
}