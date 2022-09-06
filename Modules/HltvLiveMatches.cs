using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvLiveMatches 
    {
        public static async Task<List<MatchUpcoming>> GetLiveMatches()
        {
            Directory.CreateDirectory("./cache/matches");
            try
            {
                JArray req = await Tools.RequestApiJArray("getMatches", new List<string>(), new List<string>());
                List<MatchUpcoming> matches = new();
                foreach (JObject jObj in req)
                {
                    MatchUpcoming match = new MatchUpcoming(jObj);
                    if (bool.Parse(jObj.GetValue("live").ToString()) && match.team1 != null) { matches.Add(match); }
                }
                File.WriteAllText("./cache/matches/liveMatches.json", JArray.FromObject(matches).ToString());
                return matches;
            }
            catch (HltvApiException) { throw; }
        }

        private async static Task<Embed> GetLiveMatchesEmbed()
        {
            List<MatchUpcoming> matches = new();
            try
            {
                matches = await GetLiveMatches();
            }
            catch(HltvApiException) { throw; }

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
                builder.AddField($"{emote} {match.team1.name} vs. {match.team2.name}",
                    $"[matchpage]({match.link})\n" +
                    $"event: [{match.eventObj.name}]({match.eventObj.link})\n");
#if DEBUG
                builder.WithFooter("React with the matchnumber to add a live scoreboard to your server!");
#endif
            }
            return builder.Build();
        }

        public async static Task SendLiveMatchesEmbed(SocketSlashCommand arg)
        {
            await arg.DeferAsync();
            try
            {
                Embed embed = await GetLiveMatchesEmbed();
                await arg.DeleteOriginalResponseAsync();
                await arg.Channel.SendMessageAsync(embed: embed);
            }
            catch (HltvApiException e) { await arg.ModifyOriginalResponseAsync(msg => msg.Embed = ErrorHandling.GetErrorEmbed(e)); }
        }
    }
}
