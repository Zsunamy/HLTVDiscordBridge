using System.Collections.Generic;
using System.Linq;
using Discord;
using HLTVDiscordBridge.Modules;

namespace HLTVDiscordBridge.Shared;

public class FullPlayer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Ign { get; set; }
    public string Image { get; set; }
    public string Age { get; set; }
    public string Twitter { get; set; }
    public string Twitch { get; set; }
    public string Instagram { get; set; }
    public Country Country { get; set; }
    public FullPlayerTeam Team { get; set; }
    public Achievement[] Achievements { get; set; }
    public TeamMembership[] TeamMemberships { get; set; }
    public News[] News { get; set; }

    public Embed ToEmbed(PlayerStats stats)
    {
        EmbedBuilder builder = new();
        if (stats.Image != null)
        {
            builder.WithThumbnailUrl(stats.Image);
        }
        if (stats.Id != 0 && stats.Ign != null) 
        {
            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", 
                $"https://hltv.org/player/{stats.Id}/{stats.Ign}");
        }
        if (stats.Country.Code != null && stats.Ign != null)
        {
            builder.WithTitle(stats.Ign + $" :flag_{stats.Country.Code}:");
        }
        if (stats.Name != null)
        {
            builder.AddField("Name:", stats.Name, true);
        }
        if (stats.Age != null)
        {
            builder.AddField("Age:", stats.Age, true);
        }
        else
        {
            builder.AddField("\u200b", "\u200b");
        }
        if (stats.Team != null)
        {
            builder.AddField("Team:", $"[{stats.Team.Name}]({stats.Team.Link})", true);
        }
        else
        {
            builder.AddField("Team:", "none");
        }
        if (stats.OverviewStatistics != null) 
        {
            builder.AddField("Stats:", "Maps played:\nKills/Deaths:\nHeadshot %:\nADR:\nKills per round:\nAssists per round:\nDeaths per round:", true);
            builder.AddField("\u200b", $"{stats.OverviewStatistics.MapsPlayed}\n" +
                                       $"{stats.OverviewStatistics.Kills}/{stats.OverviewStatistics.Deaths} ({stats.OverviewStatistics.KdRatio})\n" +
                                       $"{stats.OverviewStatistics.Headshots}\n{stats.OverviewStatistics.DamagePerRound}\n" + 
                                       $"{stats.OverviewStatistics.KillsPerRound}\n" +
                                       $"{stats.OverviewStatistics.AssistsPerRound}\n" +
                                       $"{stats.OverviewStatistics.DeathsPerRound}", true);
        }
        builder.WithCurrentTimestamp();

        if(Achievements.Length != 0)
        {
            List<string> achievements = new();
            foreach (Achievement achievement in Achievements.TakeWhile(_ => string.Join("\n", achievements).Length <= 600))
            {
                achievements.Add($"[{achievement.EventObj.Name}]({achievement.EventObj.Link}) finished: {achievement.Place}");
            }
            builder.AddField("Achievements:", string.Join("\n", achievements));

        }
        else
        {
            builder.AddField("Achievements:", $"none");
        }
        builder.WithFooter(Tools.GetRandomFooter());

        bool tracked = false;
        foreach (PlayerReq plReq in StatsUpdater.StatsTracker.Players.Where(plReq => plReq.Name == stats.Ign))
        {
            StatsUpdater.StatsTracker.Players.Remove(plReq);
            plReq.Reqs += 1;
            StatsUpdater.StatsTracker.Players.Add(new PlayerReq(stats.Ign, stats.Id, plReq.Reqs));
            tracked = true;
            break;
        }
        if (!tracked)
        {
            StatsUpdater.StatsTracker.Players.Add(new PlayerReq(stats.Ign, stats.Id, 1));
        }
        StatsUpdater.UpdateStats();
        return builder.Build();
    }
}