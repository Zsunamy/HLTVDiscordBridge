using System;
using Discord;

namespace HLTVDiscordBridge.Modules;

public class ApiError : Exception
{
    public string Id { get; set; }
    public string Error { get; set; }
    
    public Embed ToEmbed()
    {
        EmbedBuilder builder = new();
        builder.WithColor(Color.Red)
            .WithTitle("Error")
            .WithDescription($"`{Error}`\nIf you think this error shouldn't have happened, please contact us on our [discord server](https://discord.gg/r2U23xu4z5)")
            .WithFooter(Id)
            .WithCurrentTimestamp();
        return builder.Build();
    }
}