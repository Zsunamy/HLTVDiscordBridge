using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;
using HLTVDiscordBridge.Shared;

namespace HLTVDiscordBridge.Modules;

public static class HltvRanking
{
    private const string Path = "./cache/ranking.json";

    private static readonly string[] Months =
        { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };

    public static async Task UpdateTeamRanking()
    {
        if (DateTime.Now.DayOfWeek == DayOfWeek.Monday || !File.Exists(Path))
        {
            GetTeamRanking request = new();
            Tools.SaveToFile(Path, await request.SendRequest<TeamRanking[]>());
        }
    }

    public static async Task SendRanking(SocketSlashCommand cmd)
    {
        Embed embed;
        DateTime date = DateTime.Now;
        try
        {
            GetTeamRanking request = null;
            foreach (SocketSlashCommandDataOption opt in cmd.Data.Options)
            {
                request = new GetTeamRanking();
                switch (opt.Name)
                {
                    case "date" when DateTime.TryParse(opt.Value.ToString(), out date):
                    {
                        date = GetLastMonday(date);
                        request.Year = date.Year;
                        request.Month = Months[date.Month - 1];
                        request.Day = date.Day;
                        break;
                    }
                    case "region":
                    {
                        string region = opt.Value.ToString();
                        if (region!.Contains('-'))
                        {
                            region = "";
                            foreach (string str in region.Split('-'))
                            {
                                region += $"{str} ";
                            }
                        }
                        request.Country = region.ToLower();
                        break;
                    }
                }
            }

            TeamRanking[] ranking;
            if (request == null)
            {
                ranking = Tools.ParseFromFile<TeamRanking[]>(Path);
            }
            else
            {
                ranking = await request.SendRequest<TeamRanking[]>();
            }
            embed = GetRankingEmbed(ranking, date);
        }
        catch (ApiError ex)
        {
            embed = ex.ToEmbed();
        }
        catch (DeploymentException ex)
        {
            embed = ex.ToEmbed();
        }

            
        StatsUpdater.StatsTracker.MessagesSent += 1;
        StatsUpdater.UpdateStats();
        await cmd.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
    }

    private static Embed GetRankingEmbed(TeamRanking[] ranking, DateTime date)
    {
        EmbedBuilder embedBuilder = new();
        string val = "";
        const int maxTeams = 10;
        foreach (TeamRanking rank in ranking.Take(maxTeams))
        {
            string development;
            if (rank.IsNew)
            {
                development = "(🆕)";
            }
            else
            {
                development = rank.Change switch
                {
                    < 0 => "(⬇️ " + Math.Abs(rank.Change) + ")",
                    > 0 => "(⬆️ " +  rank.Change + ")",
                    _ => "(⏺️ 0)",
                };
            }
            val += $"{Array.IndexOf(ranking, rank) + 1}.\t[{rank.Team.Name}]({rank.Team.Link}) {development}\n";
        }
        embedBuilder.WithTitle($"TOP {Math.Max(maxTeams, ranking.Length)} {date.ToShortDateString()}")
            .AddField("teams:", val)
            .WithColor(Color.Blue)
            .WithFooter(Tools.GetRandomFooter());
        return embedBuilder.Build();
    }

    private static DateTime GetLastMonday(DateTime date)
    {
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(-1);
        }
        return date;
    }
}