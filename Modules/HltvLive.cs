using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class HltvLive : ModuleBase<SocketCommandContext>
    {
        private static async Task<List<JObject>> GetLiveMatches()
        {            
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            List<JObject> matches = new();
            foreach (JToken jTok in jArr)
            {
                JObject jObj = JObject.Parse(jTok.ToString());
                if (bool.Parse(jObj.GetValue("live").ToString())) 
                {
                    uint matchId = uint.Parse(JObject.Parse(jTok.ToString()).GetValue("id").ToString());                    
                    if(!File.Exists($"./cache/livematches/{matchId}.json"))
                    {
                        FileStream fs = File.Create($"./cache/livematches/{matchId}.json");
                        fs.Close();
                        JObject newLiveMatch = await HltvResults.GetMatchByMatchId(matchId);
                        File.WriteAllText($"./cache/livematches/{matchId}.json", newLiveMatch.ToString());
                        matches.Add(newLiveMatch);
                    } else
                    {
                        JObject cachedLiveMatch = JObject.Parse(File.ReadAllText($"./cache/livematches/{matchId}.json"));
                        matches.Add(cachedLiveMatch);
                    }
                }
                else { break; }
            }
            return matches;
        }
        private static async Task<(Embed, ushort)> GetLiveMatchesEmbed()
        {
            List<JObject> matches = await GetLiveMatches();
            EmbedBuilder builder = new();
            builder.WithTitle("LIVE MATCHES")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();
            foreach(JObject jObj in matches)
            {
                Emoji emote = new((matches.IndexOf(jObj) + 1).ToString() + "️⃣");
                JObject team1 = JObject.Parse(jObj.GetValue("team1").ToString());
                JObject team2 = JObject.Parse(jObj.GetValue("team2").ToString());
                JObject eventObj = JObject.Parse(jObj.GetValue("event").ToString());
                string streamLink;
                if(JArray.Parse(jObj.GetValue("streams").ToString()).ToString() == "[]") { streamLink = "no livestream available"; }
                else { streamLink = "[livestream](https://www.hltv.org" + JObject.Parse(JArray.Parse(jObj.GetValue("streams").ToString())[0].ToString()).GetValue("link").ToString() + ")"; }                
                string matchpageLink = $"https://www.hltv.org/matches/{jObj.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-')}-vs-{team2.GetValue("name").ToString().Replace(' ', '-')}-" +
                    $"{eventObj.GetValue("name").ToString().Replace(' ', '-')}";
                string eventLink = $"https://hltv.org/events/{eventObj.GetValue("id")}/{eventObj.GetValue("name").ToString().Replace(' ', '-')}";
                builder.AddField($"{emote} {team1.GetValue("name")} vs. {team2.GetValue("name")}", 
                    $"{streamLink}\n" +
                    $"[matchpage]({matchpageLink})\n" +
                    $"event: [{eventObj.GetValue("name")}]({eventLink})\n");
#if DEBUG
                builder.WithFooter("React with the matchnumber to add a live scoreboard to your server!");
#endif
            }
            return (builder.Build(), ushort.Parse(matches.Count.ToString()));
        }

        public static void StartScoreboard(IUserMessage msg, Emoji emote, SocketGuild guild)
        {
            foreach(EmbedField field in msg.Embeds.First().Fields)
            {
                if(field.Name.Contains(emote.ToString()))
                {
                    uint matchId = uint.Parse(field.Value.Split("\n")[1].Substring(41,7));
                    _ = new Scoreboard(matchId, guild, field.Name.Substring(4));
                }
            }
        }

        #region COMMANDS
        [Command("live"), Alias("stream", "streams")]
        public async Task DisplayLiveMatches()
        {
            EmbedBuilder builder = new();
            builder.WithTitle("Your request is loading!")
                   .WithDescription("This may take up to 30 seconds")
                   .WithCurrentTimestamp();
            var LoadingMsg = await Context.Channel.SendMessageAsync(embed: builder.Build());
            IDisposable typingState = Context.Channel.EnterTypingState();
            (Embed, ushort) res = await GetLiveMatchesEmbed();
            typingState.Dispose();
            await LoadingMsg.DeleteAsync();
            RestUserMessage msg = (RestUserMessage)await ReplyAsync(embed: res.Item1);
            for(int i = 1; i <= res.Item2; i++)
            {
                Emoji emote = new(i.ToString() + "️⃣");
#if DEBUG
                await msg.AddReactionAsync(emote);
#endif
            }
        }
        #endregion

    }
}
