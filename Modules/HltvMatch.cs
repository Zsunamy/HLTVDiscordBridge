using System.Collections.Generic;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvMatch
{
    public static async Task<Match> GetMatch(string url)
    {
        GetMatch request = new(Tools.GetIdFromUrl(url));
        return await request.SendRequest<Match>("getMatch");
    }
}