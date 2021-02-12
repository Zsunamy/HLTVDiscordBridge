using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Hltv : ModuleBase<SocketCommandContext>
    {        
        private Config _cfg = new Config();

        /// <summary>
        /// Gets a match by its matchlink
        /// </summary>
        /// <param name="matchLink">Matchlink</param>
        /// <returns>Matchstats as JObject and HTTP error code</returns>
        private async Task<(JObject, ushort)> getMatchByMatchlink (string matchLink)
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/results");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr;
            try { jArr = JArray.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return (null, 503); }

            JObject jObj = null;
            foreach (JToken jTok in jArr)
            {
                jObj = JObject.Parse(jTok.ToString());
                if (matchLink.Contains(jObj.GetValue("id").ToString())) { return (jObj, 200); }
            }
            return (null, 404);
        }

        /// <summary>
        /// Gets the latest Match, if it isn't already tracked and has more than the required stars (s. Config)
        /// </summary>
        /// <returns>The latest match as JObject</returns>
        public async Task<JObject> GetResults()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/results");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);

            string httpRes = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr;
            try { jArr = JArray.Parse(httpRes); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return null; }

            Directory.CreateDirectory("./cache");
            if (!File.Exists("./cache/matchIDs.txt"))
            {
                var stream = File.Create("./cache/matchIDs.txt");                
                stream.Close();                    
                
                foreach (JToken jToken in jArr)
                {
                    File.AppendAllText("./cache/matchIDs.txt", JObject.Parse(jToken.ToString()).GetValue("id").ToString() + "\n");
                }
                return null;
            }
            string matchIDs = File.ReadAllText("./cache/matchIDs.txt"); 

            foreach(JToken jToken in jArr)
            {
                string MatchID = JObject.Parse(jToken.ToString()).GetValue("id").ToString();
                if (!matchIDs.Contains(MatchID))
                {
                    File.AppendAllText("./cache/matchIDs.txt", JObject.Parse(jToken.ToString()).GetValue("id").ToString() + "\n");
                    return JObject.Parse(jToken.ToString());
                }            
                else
                {
                    continue;
                }
            }
            return null;
        }
        /// <summary>
        /// Gets the stats of a match
        /// </summary>
        /// <param name="res">JObject containing the match</param>
        /// <returns>Embed</returns>
        public Embed GetStats(JObject res)
        {            
            if(res == null)
            {
                return null;
            }
            JObject data = res;
            JObject eventinfo = JObject.Parse(data.GetValue("event").ToString());
            JObject team1 = JObject.Parse(data.GetValue("team1").ToString());
            JObject team2 = JObject.Parse(data.GetValue("team2").ToString());
            string[] result = data.GetValue("result").ToString().Split(" - ");
            string latmatchid = $"https://www.hltv.org/matches/{data.GetValue("id")}/{team1.GetValue("name").ToString().Replace(' ', '-')}-vs-" +
                $"{team2.GetValue("name").ToString().Replace(' ', '-')}-{eventinfo.GetValue("name").ToString().Replace(' ', '-')}";            

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"{team1.GetValue("name")} vs. {team2.GetValue("name")}")
                .WithColor(Color.Red)
                .AddField("event:", eventinfo.GetValue("name"))
                .AddField("maps:", data.GetValue("format"))
                .AddField(team1.GetValue("name").ToString(), result[0], true)
                .AddField(team2.GetValue("name").ToString(), result[1], true)
                .WithAuthor("full details by hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", latmatchid)
                .WithCurrentTimestamp();

            return builder.Build();
        }
        /// <summary>
        /// Send the latest match as Embed in a Discord SocketTextChannel
        /// </summary>
        /// <param name="channel">List of all Channels in which the embed should be sent</param>
        public async Task AktHLTV(List<SocketTextChannel> channels, DiscordSocketClient client)
        {
            JObject res = await GetResults();
            
            if (res != null)
            {
                Embed embed = GetStats(res);
                foreach(SocketTextChannel channel in channels)
                {
                    if (ushort.Parse(res.GetValue("stars").ToString()) >= _cfg.GetServerConfig(channel).MinimumStars)
                    {
//#if RELEASE
                        try { RestUserMessage msg = await channel.SendMessageAsync("", false, embed); await msg.AddReactionAsync(await _cfg.GetEmote(client)); }
                        catch (Discord.Net.HttpException)
                        {
                            Console.WriteLine($"not enough permission in channel {channel}");
                        }
//#endif
                    }
                    await UpdateUpcomingMatches();
                }
            }
        }


        /// <summary>
        /// Gets stats of a match with given matchlink
        /// </summary>
        /// <param name="matchlink">link of the match</param>
        /// <returns>MatchStats</returns>
        public async Task<JObject> GetPLMessage(string matchid)
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/match/" + matchid);
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpResPLStats = await httpResponse.Content.ReadAsStringAsync();
            return JObject.Parse(httpResPLStats);
        }
        /// <summary>
        /// Converts a JArray of a MatchStats into a discord Embed
        /// </summary>
        /// <param name="matchlink"></param>
        /// <returns>Discord Embed</returns>
        public async Task<Embed> GetPLStats(string matchlink)
        {
            Console.WriteLine(matchlink);
            EmbedBuilder builder = new EmbedBuilder();
            //get team names from cached matches
            string team1name = null;
            string team2name = null;
            Console.WriteLine(matchlink.Substring(29, 7));

            JObject matchStats = await GetPLMessage(matchlink.Substring(29, 7));
            team1name = JObject.Parse(matchStats.GetValue("team1").ToString()).GetValue("name").ToString();
            team2name = JObject.Parse(matchStats.GetValue("team2").ToString()).GetValue("name").ToString();
            JArray team1PL = JArray.Parse(JObject.Parse(matchStats.GetValue("players").ToString()).GetValue("team1").ToString());
            JArray team2PL = JArray.Parse(JObject.Parse(matchStats.GetValue("players").ToString()).GetValue("team2").ToString());
            var PL0 = JObject.Parse(team1PL[0].ToString());
            var PL1 = JObject.Parse(team1PL[1].ToString());
            var PL2 = JObject.Parse(team1PL[2].ToString());
            var PL3 = JObject.Parse(team1PL[3].ToString());
            var PL4 = JObject.Parse(team1PL[4].ToString());
            var PL5 = JObject.Parse(team2PL[0].ToString());
            var PL6 = JObject.Parse(team2PL[1].ToString());
            var PL7 = JObject.Parse(team2PL[2].ToString());
            var PL8 = JObject.Parse(team2PL[3].ToString());
            var PL9 = JObject.Parse(team2PL[4].ToString());

            
            builder.WithTitle($"PLAYERSTATS ({team1name} vs. {team2name})")
                .WithColor(Color.Red)
                .AddField($"players ({team1name}):", PL0.GetValue("name").ToString() + "\n" + PL1.GetValue("name").ToString() + "\n" + PL2.GetValue("name").ToString() + "\n" + PL3.GetValue("name").ToString() + "\n" + PL4.GetValue("name").ToString(), true)
                .AddField($"players ({team2name}):", PL5.GetValue("name").ToString() + "\n" + PL6.GetValue("name").ToString() + "\n" + PL7.GetValue("name").ToString() + "\n" + PL8.GetValue("name").ToString() + "\n" + PL9.GetValue("name").ToString(), true)
                .WithAuthor("full stats on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", matchlink)
                .WithCurrentTimestamp();

            return builder.Build();
        }

        /// <summary>
        /// Triggerd by reaction. Sends the Embed
        /// </summary>
        /// <returns></returns>
        public async Task stats(string matchlink, ITextChannel channel)
        {
            await channel.SendMessageAsync("", false, await GetPLStats(matchlink));
        }

        /// <summary>
        /// Gets upcoming HLTV matches and their star rating and saves them in ./cache/upcoming.json
        /// </summary>
        /// <returns>All upcoming matches</returns>
        public async Task UpdateUpcomingMatches()
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/matches");
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            string httpResult = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr;
            try { jArr = JArray.Parse(httpResult); }
            catch (Newtonsoft.Json.JsonReaderException) { Console.WriteLine($"{DateTime.Now.ToString().Substring(11)}API\t API down"); return; }
            JArray myJArr = new JArray();
            Directory.CreateDirectory("./cache");
            if (!File.Exists("./cache/upcoming.json"))
            {
                FileStream fs = File.Create("./cache/upcoming.json");
                fs.Close();
                File.WriteAllText("./cache/upcoming.json", jArr.ToString());
                return;
            }
            else
            {
                myJArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
                //Console.WriteLine(myJArr);
            }
            
            foreach(JToken jToken in jArr)
            {
                if (!myJArr.ToString().Contains(JObject.Parse(jToken.ToString()).GetValue("link").ToString()))
                {
                    myJArr.Add(jToken);
                    /*if(myJArr.Contains(JObject.Parse(jToken.ToString()).GetValue("id").ToString()))
                    {
                        myJArr.IndexOf()
                    }*/
                }          
            }
            File.WriteAllText("./cache/upcoming.json", myJArr.ToString());
        } 
    }
}
