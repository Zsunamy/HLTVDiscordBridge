using Discord;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvApiException : Exception
    {
        public string Id;
        public override string Message { get; }
        public HltvApiException(JObject jObject)
        {
            Message = jObject.GetValue("error").ToString();
            Id = jObject.GetValue("id").ToString();
        }
    }

    public static class ErrorHandling
    {
        public static Embed GetErrorEmbed(HltvApiException ex)
        {
            EmbedBuilder builder = new();
            builder.WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription($"`{ex.Message}`\nIf you think this error shouldn't have happened, please contact us on our [discord server](https://discord.gg/r2U23xu4z5)")
                .WithFooter(ex.Id)
                .WithCurrentTimestamp();
            return builder.Build();
        }
    }
}
