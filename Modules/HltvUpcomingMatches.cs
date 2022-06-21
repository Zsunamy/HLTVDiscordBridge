using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public static class HltvUpcomingMatches
    {
        public static async Task<List<MatchUpcoming>> GetUpcomingMatches()
        {
            Directory.CreateDirectory("./cache/matches");
            try
            {                
                JArray req = await Tools.RequestApiJArray("getMatches", new List<string>(), new List<string>());
                List<MatchUpcoming> matches = new();
                foreach (JObject jObj in req)
                {
                    MatchUpcoming match = new MatchUpcoming(jObj);
                    if (!bool.Parse(jObj.GetValue("live").ToString()) && match.team1 != null) { matches.Add(match); }
                }
                File.WriteAllText("./cache/matches/upcomingMatches.json", JArray.FromObject(matches).ToString());
                return matches;
            }
            catch (HltvApiException) { throw; }
        }
        public static List<MatchUpcoming> GetUpcomingMatchesByDate(List<MatchUpcoming> matches, DateTime date)
        {
            List<MatchUpcoming> newMatches = new();
            foreach (MatchUpcoming match in matches)
            {
                if(UnixTimeStampToDateTime(match.date).Date == date.Date)
                {
                    newMatches.Add(match);
                }               
            }
            return newMatches;
        }
        public static List<MatchUpcoming> GetUpcomingMatchesByValue(List<MatchUpcoming> matches, string val)
        {
            List<MatchUpcoming> newMatches = new();
            val = val.ToLower();
            foreach (MatchUpcoming match in matches)
            {
                if(match.ToString().ToLower().Contains(val))
                {
                    newMatches.Add(match);
                }
            }
            return newMatches;
        }
        public static async Task<List<MatchUpcoming>> GetLiveMatches()
        {
            Directory.CreateDirectory("./cache/matches");
            try
            {
                JArray req = await Tools.RequestApiJArray("getMatches", new List<string>(), new List<string>());
                List<MatchUpcoming> matches = new();
                foreach (JObject jObj in req)
                {
                    if (bool.Parse(jObj.GetValue("live").ToString())) { matches.Add(new MatchUpcoming(jObj)); }
                }
                File.WriteAllText("./cache/matches/liveMatches.json", JArray.Parse(matches.ToString()).ToString());
                return matches;
            }
            catch (HltvApiException) { throw; }
        }

        public static async Task<Embed> GetUpcomingMatchesEmbed(SocketSlashCommand command)
        {
            try
            {
                List<MatchUpcoming> matches = await GetUpcomingMatches();
                if(command.Data.Options.Count != 0)
                {
                    string param = command.Data.Options.First().Value.ToString();
                    if (DateTime.TryParse(param, out DateTime date))
                    {
                        matches = GetUpcomingMatchesByDate(matches, date);
                    }
                    else
                    {
                        matches = GetUpcomingMatchesByValue(matches, param);
                    }
                }    
                EmbedBuilder builder = new();
                for(int i = 0; i < 3 && i < matches.Count; i++)
                {
                    builder.AddField("match:", $"[{matches.ElementAt(i).team1.name}]({matches.ElementAt(i).team1.link}) vs. [{matches.ElementAt(i).team2.name}]({matches.ElementAt(i).team2.link})", true);
                    builder.AddField("time:", $"{UnixTimeStampToDateTime(matches.ElementAt(i).date).ToShortDateString()} UTC", true);
                    builder.AddField("\u200b", "\u200b", true);
                    builder.AddField("event:", $"[{matches.ElementAt(i).eventObj.name}]({matches.ElementAt(i).eventObj.link})", true);
                    builder.AddField("format:", GetFormatFromAcronym(matches.ElementAt(i).format), true);
                    builder.AddField("\u200b", "\u200b", true);
                    builder.AddField("details:", $"[click here for more details]({matches.ElementAt(i).link})");
                    if(i==2)
                    {
                        builder.WithFooter($"and {matches.Count - 3} more");
                    }
                    else if(i < matches.Count - 1)
                    {
                        builder.AddField("\u200b", "\u200b");
                    }
                }
                builder.WithCurrentTimestamp()
                .WithColor(Color.Blue);
                if(matches.Count < 3) { builder.WithFooter(Tools.GetRandomFooter()); }
                return builder.Build();
            }
            catch(HltvApiException) { throw; }
        }

        public static async Task SendUpcomingMatches(SocketSlashCommand arg)
        {
            await arg.DeferAsync(); Embed embed;
            try { embed = await GetUpcomingMatchesEmbed(arg); }
            catch(HltvApiException e) { embed = ErrorHandling.GetErrorEmbed(e); }
            await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
        }
        private static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
        {
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(double.Parse(unixTimeStamp.ToString())).ToUniversalTime();
            dtDateTime = dtDateTime.AddHours(1);
            return dtDateTime;
        }
        private static string GetFormatFromAcronym(string req)
        {
            return req switch
            {
                "bo1" => "Best of 1",
                "bo3" => "Best of 3",
                "bo5" => "Best of 5",
                "bo7" => "Best of 7",
                _ => "n.A",
            };
        }
    }
}
