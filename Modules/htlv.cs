using Discord;
using Discord.Commands;
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
        private string httpRes = null;
        private string httpResPLStats = null;
        private Config _cfg = new Config();

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

            httpRes = await httpResponse.Content.ReadAsStringAsync();
            JArray jArr = JArray.Parse(httpRes);

            Directory.CreateDirectory("./cache");
            if (!File.Exists("./cache/matchIDs.txt"))
            {
                var stream = File.Create("./cache/matchIDs.txt");                
                stream.Close();                    
                
                foreach (JToken jToken in jArr)
                {
                    File.AppendAllText("./cache/matchIDs.txt", JObject.Parse(jToken.ToString()).GetValue("matchId").ToString() + "\n");
                }
                return null;
            }
            string matchIDs = File.ReadAllText("./cache/matchIDs.txt"); 

            foreach(JToken jToken in jArr)
            {
                string MatchID = JObject.Parse(jToken.ToString()).GetValue("matchId").ToString();
                if (!matchIDs.Contains(MatchID))
                {
                    File.AppendAllText("./cache/matchIDs.txt", JObject.Parse(jToken.ToString()).GetValue("matchId").ToString() + "\n");
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
            var data = res;
            var team1 = JObject.Parse(data["team1"].ToString());
            var team2 = JObject.Parse(data["team2"].ToString());
            string latmatchid = data.GetValue("matchId").ToString();            

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"{team1.GetValue("name")} vs. {team2.GetValue("name")}")
                .WithColor(Color.Red)
                .AddField("event:", data.GetValue("event"))
                .AddField("maps:", data.GetValue("maps"))
                .AddField(team1.GetValue("name").ToString(), team1.GetValue("result"), true)
                .AddField(team2.GetValue("name").ToString(), team2.GetValue("result"), true)
                .WithAuthor("full details by hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://www.hltv.org" + latmatchid)
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
                    JArray upcoming = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
                    foreach (JToken jTok in upcoming)
                    {
                        string MatchID = res.GetValue("matchId").ToString();
                        JObject link = JObject.Parse(jTok.ToString());
                        if (MatchID.Contains(link.GetValue("link").ToString()))
                        {
                            if (ushort.Parse(link.GetValue("stars").ToString()) >= _cfg.GetServerConfig(channel).MinimumStars)
                            {                                
                                try { var msg = await channel.SendMessageAsync("", false, embed); await msg.AddReactionAsync(await _cfg.GetEmote(client)); } 
                                catch(Discord.Net.HttpException)
                                {
                                    Console.Write($"not enough permission in channel {channel}");
                                }
                                await UpdateUpcomingMatches();
                            }
                        }
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
        public async Task<JArray> GetPLMessage(string matchlink)
        {
            var URI = new Uri("https://hltv-api-steel.vercel.app/api/" + matchlink.Substring(20));
            HttpClient http = new HttpClient();

            http.BaseAddress = URI;

            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            httpResPLStats = await httpResponse.Content.ReadAsStringAsync();
            return JArray.Parse(httpResPLStats);
        }
        /// <summary>
        /// Converts a JArray of a MatchStats into a discord Embed
        /// </summary>
        /// <param name="matchlink"></param>
        /// <returns>Discord Embed</returns>
        public async Task<Embed> GetPLStats(string matchlink)
        {
            //get team names from cached matches
            string team1name = null;
            string team2name = null;
            JArray jsonArray1 = JArray.Parse(httpRes);
            foreach(JToken tok in jsonArray1)
            {
                var jobj = JObject.Parse(tok.ToString());
                if(jobj.GetValue("matchId").ToString() == matchlink.Substring(20))
                {
                    team1name = JObject.Parse(jobj["team1"].ToString()).GetValue("name").ToString();
                    team2name = JObject.Parse(jobj["team2"].ToString()).GetValue("name").ToString();
                }
            }

            var URI = new Uri("https://hltv-api.vercel.app/api" + matchlink.Substring(20));
            HttpClient http = new HttpClient();
            http.BaseAddress = URI;
            HttpResponseMessage httpResponse = await http.GetAsync(URI);
            httpResPLStats = await httpResponse.Content.ReadAsStringAsync();

            JArray jsonArray = await GetPLMessage(matchlink);
            var PL0 = JObject.Parse(jsonArray[0].ToString());
            var PL1 = JObject.Parse(jsonArray[1].ToString());
            var PL2 = JObject.Parse(jsonArray[2].ToString());
            var PL3 = JObject.Parse(jsonArray[3].ToString());
            var PL4 = JObject.Parse(jsonArray[4].ToString());
            var PL5 = JObject.Parse(jsonArray[5].ToString());
            var PL6 = JObject.Parse(jsonArray[6].ToString());
            var PL7 = JObject.Parse(jsonArray[7].ToString());
            var PL8 = JObject.Parse(jsonArray[8].ToString());
            var PL9 = JObject.Parse(jsonArray[9].ToString());

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"PLAYERSTATS ({team1name} vs. {team2name})")
                .WithColor(Color.Red)
                .AddField($"players ({team1name}):", PL0.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL1.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL2.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL3.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL4.GetValue("playerName").ToString().Split(' ')[1], true)
                .AddField("K/D:", PL0.GetValue("kills") + "/" + PL0.GetValue("deaths") + "\n" + PL1.GetValue("kills") + "/" + PL1.GetValue("deaths") + "\n" + PL2.GetValue("kills") + "/" + PL2.GetValue("deaths") + "\n" + PL3.GetValue("kills") + "/" + PL3.GetValue("deaths") + "\n" + PL4.GetValue("kills") + "/" + PL4.GetValue("deaths"), true)
                .AddField("rating:", PL0.GetValue("rating") + "\n" + PL1.GetValue("rating") + "\n" + PL2.GetValue("rating") + "\n" + PL3.GetValue("rating") + "\n" + PL4.GetValue("rating"), true)
                .AddField($"players ({team2name}):", PL5.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL6.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL7.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL8.GetValue("playerName").ToString().Split(' ')[1] + "\n" + PL9.GetValue("playerName").ToString().Split(' ')[1], true)
                .AddField("K/D:", PL5.GetValue("kills") + "/" + PL5.GetValue("deaths") + "\n" + PL6.GetValue("kills") + "/" + PL6.GetValue("deaths") + "\n" + PL7.GetValue("kills") + "/" + PL7.GetValue("deaths") + "\n" + PL8.GetValue("kills") + "/" + PL8.GetValue("deaths") + "\n" + PL9.GetValue("kills") + "/" + PL9.GetValue("deaths"), true)
                .AddField("rating:", PL5.GetValue("rating") + "\n" + PL6.GetValue("rating") + "\n" + PL7.GetValue("rating") + "\n" + PL8.GetValue("rating") + "\n" + PL9.GetValue("rating"), true)
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
            JArray jArr = JArray.Parse(httpResult);
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
                }          
            }
            File.WriteAllText("./cache/upcoming.json", myJArr.ToString());
        } 
    }
}
