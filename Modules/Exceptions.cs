using System;
using System.Net.Http;
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

public class DeploymentException : Exception
{
    private readonly HttpResponseMessage _httpMessage;

    public DeploymentException(HttpResponseMessage message)
    {
        _httpMessage = message;
    }

    public Embed ToEmbed()
    {
        EmbedBuilder builder = new();
        builder.WithColor(Color.Red)
            .WithTitle("Error")
            .WithDescription($"`{_httpMessage.Content}`\nIf you think this error shouldn't have happened, please contact us on our [discord server](https://discord.gg/r2U23xu4z5)")
            .WithFooter(Tools.GetRandomFooter())
            .WithCurrentTimestamp();
        return builder.Build();
    }
}