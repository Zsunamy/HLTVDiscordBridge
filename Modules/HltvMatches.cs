using System;
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
        await Program.Log(new LogMessage(LogSeverity.Verbose, nameof(HltvMatches),
            $"fetched matches ({watch.ElapsedMilliseconds}ms)"));
    }
    
    public static async Task SendLiveMatches(SocketSlashCommand cmd)
    {
        Embed embed = GetLiveMatchesEmbed(Tools.ParseFromFile<MatchPreview[]>(Path).Where(x => x.Live).ToArray());
        await cmd.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }

    public static async Task SendUpcomingMatches(SocketSlashCommand cmd)
    {
        Embed embed;
        GetMatches request = new();
        try
        {
            foreach (SocketSlashCommandDataOption opt in cmd.Data.Options)
            {
                switch (opt.Name)
                {
                    case "team":
                        int? id = await HltvTeams.GetIdFromDatabase(opt.Value.ToString()!.ToLower());
                        if (id != null)
                        {
                            request.TeamIds = new[] { (int)id };
                            break;
                        }
                        GetTeamByName teamRequest = new GetTeamByName { Name = opt.Value.ToString()!.ToLower() };
                        request.TeamIds = new[] { (await teamRequest.SendRequest<FullTeam>()).Id };
                        break;
                    case "event":
                        GetEventByName eventRequest = new GetEventByName { Name = opt.Value.ToString()!.ToLower() };
                        request.EventId = (await eventRequest.SendRequest<FullEvent>()).Id;
                        break;
                }
            }

            MatchPreview[] matches = cmd.Data.Options.Any() ? await request.SendRequest<MatchPreview[]>() : Tools.ParseFromFile<MatchPreview[]>(Path);
            embed = GetUpcomingMatchesEmbed(matches);

        }
        catch (ApiError ex)
        {
            embed = ex.ToEmbed();
        }
        catch (DeploymentException ex)
        {
            embed = ex.ToEmbed();
        }

        await cmd.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }

    private static Embed GetUpcomingMatchesEmbed(MatchPreview[] matches)
    {
        EmbedBuilder builder = new();

        if (matches.Length == 0)
        {
            builder.WithColor(Color.Red)
                .WithTitle($"UPCOMING MATCHES")
                .WithDescription("There are no scheduled matches at the moment.")
                .WithCurrentTimestamp();
            return builder.Build();
        }

        foreach (MatchPreview match in matches.Take(3))
        {
            builder.AddField("match:", $"[{match.Team1.Name}]({match.Team1.Link}) vs. [{match.Team2.Name}]({match.Team2.Link})", true)
                .AddField("time:", $"{Tools.UnixTimeToDateTime(match.Date).ToShortDateString()} UTC", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("event:", $"[{match.Event.Name}]({match.Event.Link})", true)
                .AddField("format:", Tools.GetFormatFromAcronym(match.Format), true)
                .AddField("\u200b", "\u200b", true)
                .AddField("details:", $"[click here for more details]({match.Link})");
            
            if (Array.IndexOf(matches, match) != 2 && Array.IndexOf(matches, match) != matches.Length - 1)
                builder.AddField("\u200b", "\u200b");
        }
        
        if (matches.Length > 3)
            builder.WithFooter($"and {matches.Length - 3} more");
        else
            builder.WithFooter(Tools.GetRandomFooter());

        builder.WithCurrentTimestamp().WithColor(Color.Blue);
        return builder.Build();
    }
    private static Embed GetLiveMatchesEmbed(MatchPreview[] matches)
    {
        EmbedBuilder builder = new();
        if (matches.Length == 0)
            return builder.WithColor(Color.Red)
                .WithTitle($"LIVE MATCHES")
                .WithDescription("There are no live matches available right now")
                .WithCurrentTimestamp()
                .Build();
        
        builder.WithTitle("LIVE MATCHES")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();
        foreach (MatchPreview match in matches.Take(25))
        {
            Emoji emote = new(Array.IndexOf(matches, match) + 1 + "️⃣");
            builder.AddField($"{emote} {match.Team1.Name} vs. {match.Team2.Name}",
                $"[match]({match.Link})\n" +
                $"event: [{match.Event.Name}]({match.Event.Link})\n");
        }

        return builder.WithFooter(Tools.GetRandomFooter()).Build();
    }
}