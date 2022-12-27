using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Modules;

public static class HltvLiveMatches
{
    private const string Path = "./cache/matches/liveMatches.json";
    
    private static Embed GetLiveMatchesEmbed(List<MatchUpcoming> matches)
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
        foreach (MatchUpcoming match in matches)
        {
            Emoji emote = new((matches.IndexOf(match) + 1).ToString() + "️⃣");
            builder.AddField($"{emote} {match.Team1.Name} vs. {match.Team2.Name}",
                $"[matchpage]({match.Link})\n" +
                $"event: [{match.EventObj.Name}]({match.EventObj.Link})\n");
        }
        return builder.Build();
    }

    public static async Task SendLiveMatchesEmbed(SocketSlashCommand arg)
    {
        await arg.DeferAsync();
        Embed embed;
        GetMatches request = new();
        try
        {
            List<MatchUpcoming> matches = (List<MatchUpcoming>)(await request.SendRequest<List<MatchUpcoming>>()).Where(elem => elem.Live);
            Tools.SaveToFile(Path, matches);
            embed = GetLiveMatchesEmbed(matches);
        }
        catch (ApiError ex)
        {
            embed = ex.ToEmbed();
        }
        catch (DeploymentException ex)
        {
            embed = ex.ToEmbed();
        }

        await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }
    /*
    public static async Task<List<MatchUpcoming>> GetLiveMatches()
    {
        Directory.CreateDirectory("./cache/matches");
        GetMatches request = new();
        try
        {
            JArray req = await Tools.RequestApiJArray("getMatches", new List<string>(), new List<string>());
            List<MatchUpcoming> matches = new();
            foreach (JObject jObj in req)
            {
                MatchUpcoming match = new MatchUpcoming(jObj);
                if (bool.Parse(jObj.GetValue("live").ToString()) && match.Team1 != null) { matches.Add(match); }
            }
            File.WriteAllText("./cache/matches/liveMatches.json", JArray.FromObject(matches).ToString());
            return matches;
        }
        catch (HltvApiExceptionLegacy) { throw; }
    }
    */
}