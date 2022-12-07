using Discord;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HLTVDiscordBridge.Modules
{
    public class HltvApiExceptionLegacy : Exception
    {
        public string Id;
        public override string Message { get; }
        public HltvApiExceptionLegacy(JObject jObject)
        {
            Message = jObject.GetValue("error").ToString();
            Id = jObject.GetValue("id").ToString();
        }
    }

    public class HltvApiException : Exception
    {
        [JsonProperty("id")]
        public string Id;
        [JsonProperty("error")]
        public string Error;
    }

    public class DeploymentException : Exception
    {
        private HttpResponseMessage _httpMessage;

        public DeploymentException(HttpResponseMessage message)
        {
            _httpMessage = message;
        }
    }

    public static class ErrorHandling
    {
        public static Embed GetErrorEmbed(HltvApiExceptionLegacy ex)
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
