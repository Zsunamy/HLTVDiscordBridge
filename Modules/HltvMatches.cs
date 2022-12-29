using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvMatches
{
    private const string Path = "./cache/matches.json";

    public static async Task UpdateMatches()
    {
        Stopwatch watch = new(); watch.Start();
        GetMatches request = new();
        Tools.SaveToFile(Path, await request.SendRequest<MatchPreview[]>());
        Program.WriteLog($"{DateTime.Now.ToLongTimeString()} HLTV\t\t fetched matches ({watch.ElapsedMilliseconds}ms)");
    }
    
    public static async Task SendLiveMatchesEmbed(SocketSlashCommand arg)
    {
        await arg.DeferAsync();
        Embed embed = GetLiveMatchesEmbed(Tools.ParseFromFile<List<MatchPreview>>(Path));
        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }
    
    private static Embed GetLiveMatchesEmbed(List<MatchPreview> matches)
    {
        EmbedBuilder builder = new();
        if (matches.Count == 0)
        {
            builder.WithColor(Color.Red)
                .WithTitle($"LIVE MATCHES")
                .WithDescription("There are no live matches available right now")
                .WithCurrentTimestamp();
            return builder.Build();
        }
        builder.WithTitle("LIVE MATCHES")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();
        foreach (MatchPreview match in matches)
        {
            Emoji emote = new((matches.IndexOf(match) + 1).ToString() + "️⃣");
            builder.AddField($"{emote} {match.Team1.Name} vs. {match.Team2.Name}",
                $"[matchpage]({match.Link})\n" +
                $"event: [{match.EventObj.Name}]({match.EventObj.Link})\n");
        }
        return builder.Build();
    }
}