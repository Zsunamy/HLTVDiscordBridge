using Discord;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HLTVDiscordBridge.Requests;

namespace HLTVDiscordBridge.Modules
{
    public static class HltvUpcomingMatches
    {
        private const string OngoingPath = "./cache/matches/ongoingMatches.json";
        private const string UpcomingPath = "./cache/matches/upcomingMatches.json";
        public static async Task<IEnumerable<MatchUpcoming>> GetUpcomingMatches()
        {
            GetMatches request = new();
            List<MatchUpcoming> matches = await request.SendRequest<List<MatchUpcoming>>();
            Tools.SaveToFile(OngoingPath, matches);
            return matches.Where(match => !match.Live);
            /*Directory.CreateDirectory("./cache/matches");
            try
            {                
                JArray req = await Tools.RequestApiJArray("getMatches", new List<string>(), new List<string>());
                List<MatchUpcoming> matches = new();
                foreach (JObject jObj in req)
                {
                    MatchUpcoming match = new MatchUpcoming(jObj);
                    if (!bool.Parse(jObj.GetValue("live").ToString()) && match.Team1 != null) { matches.Add(match); }
                }
                File.WriteAllText("./cache/matches/upcomingMatches.json", JArray.FromObject(matches).ToString());
                return matches;
            }
            catch (HltvApiExceptionLegacy) { throw; }
            */
        }
        private static IEnumerable<MatchUpcoming> GetUpcomingMatchesByDate(IEnumerable<MatchUpcoming> matches, DateTime date)
        {
            return matches.Where(match => UnixTimeStampToDateTime(match.Date).Date == date.Date).ToList();
        }
        private static IEnumerable<MatchUpcoming> GetUpcomingMatchesByValue(IEnumerable<MatchUpcoming> matches, string val)
        {
            val = val.ToLower();
            return matches.Where(match => match.ToString().ToLower().Contains(val)).ToList();
        }
        public static async Task<IEnumerable<MatchUpcoming>> GetLiveMatches()
        {
            GetMatches request = new();
            List<MatchUpcoming> matches = await request.SendRequest<List<MatchUpcoming>>();
            Tools.SaveToFile(OngoingPath, matches);
            return matches.Where(match => match.Live);
            /*
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
            catch (HltvApiExceptionLegacy) { throw; }
        */
        }
        public static async Task<Embed> GetUpcomingMatchesEmbed(SocketSlashCommand command)
        {
            try
            {
                IEnumerable<MatchUpcoming> matches = await GetUpcomingMatches();
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
                for(int i = 0; i < 3 && i < matches.Count(); i++)
                {
                    builder.AddField("match:", $"[{matches.ElementAt(i).Team1.Name}]({matches.ElementAt(i).Team1.Link}) vs. [{matches.ElementAt(i).Team2.Name}]({matches.ElementAt(i).Team2.Link})", true);
                    builder.AddField("time:", $"{UnixTimeStampToDateTime(matches.ElementAt(i).Date).ToShortDateString()} UTC", true);
                    builder.AddField("\u200b", "\u200b", true);
                    builder.AddField("event:", $"[{matches.ElementAt(i).EventObj.Name}]({matches.ElementAt(i).EventObj.Link})", true);
                    builder.AddField("format:", GetFormatFromAcronym(matches.ElementAt(i).Format), true);
                    builder.AddField("\u200b", "\u200b", true);
                    builder.AddField("details:", $"[click here for more details]({matches.ElementAt(i).Link})");
                    if(i==2)
                    {
                        builder.WithFooter($"and {matches.Count() - 3} more");
                    }
                    else if(i < matches.Count() - 1)
                    {
                        builder.AddField("\u200b", "\u200b");
                    }
                }
                builder.WithCurrentTimestamp()
                .WithColor(Color.Blue);
                if(matches.Count() < 3) { builder.WithFooter(Tools.GetRandomFooter()); }
                return builder.Build();
            }
            catch(HltvApiExceptionLegacy) { throw; }
        }
        public static async Task SendUpcomingMatches(SocketSlashCommand arg)
        {
            await arg.DeferAsync(); Embed embed;
            try { embed = await GetUpcomingMatchesEmbed(arg); }
            catch(HltvApiExceptionLegacy e) { embed = ErrorHandling.GetErrorEmbed(e); }
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
