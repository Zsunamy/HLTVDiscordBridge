using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Modules
{
    class PlayerDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public ushort PlayerId { get; set; }
        public string Name { get; set; }
        public List<string> Alias { get; set; }
        public string Nationality { get; set; }
        public string Image { get; set; }
    }
    public class PlayerCard : ModuleBase<SocketCommandContext>
    {
        private static IMongoCollection<PlayerDocument> GetCollection()
        {
            MongoClient dbClient = new(Config.LoadConfig().DatabaseLink);
#if DEBUG
            IMongoDatabase db = dbClient.GetDatabase("hltv-dev");
#endif
#if RELEASE
            IMongoDatabase db = dbClient.GetDatabase("hltv");
#endif
            return db.GetCollection<PlayerDocument>("players");
        }

        public static void PlayerTest()
        {
            IMongoCollection<PlayerDocument> collection = GetCollection();
            PlayerDocument doc = new();
            List<string> alias = new();
            alias.Add("simple"); alias.Add("jürgen");
            doc.Name = "s1mple";
            doc.Alias = alias;
            collection.InsertOne(doc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playername"></param>
        /// <returns>PlayerStats as JObject, PlayerID as ushort, Achievements as JArray</returns>
        private static async Task<(JObject, ushort, JArray)> GetPlayerStats(string playername)
        {
            JObject idJObj;
            JObject statsJObj;
            //Cache Player
            Directory.CreateDirectory("./cache/playercards");
            if (Directory.Exists($"./cache/playercards/{playername.ToLower()}"))
            {
                idJObj = JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/id.json"));
                statsJObj = JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/stats.json"));
                ushort playerID = ushort.Parse(idJObj.GetValue("id").ToString());
                JArray achievements = JArray.Parse(idJObj.GetValue("achievements").ToString());
                return (statsJObj, playerID, achievements);
            } else
            {
                //Get non cached Player 

                //DATABASE
                IMongoCollection<PlayerDocument> collection = GetCollection();
                (JObject, bool) req = (null, false);
                var find1 = collection.Find(x => x.Name == playername.ToLower());
                var find2 = collection.Find(x => x.Alias.Contains(playername.ToLower()));
                if (find1.CountDocuments() == 0 && find2.CountDocuments() == 0)
                {
                    req = await Tools.RequestApiJObject("player/" + playername);
                    if (!req.Item2) { return (null, 0, null); }
                    idJObj = req.Item1;
                    if (idJObj == null) { return (null, 0, JArray.Parse("[]")); }
                    PlayerDocument doc = new();
                    doc.PlayerId = ushort.Parse(idJObj.GetValue("id").ToString());
                    doc.Name = idJObj.GetValue("ign").ToString();
                    doc.Alias = null;
                    doc.Image = idJObj.GetValue("image").ToString();
                    doc.Nationality = JObject.Parse(idJObj.GetValue("country").ToString()).GetValue("code").ToString();
                    collection.InsertOne(doc);
                }
                else
                {
                    ushort playerId;
                    if(find1.FirstOrDefault() != null) { playerId = find1.FirstOrDefault().PlayerId; }
                    else { playerId = find2.FirstOrDefault().PlayerId; playername = find2.FirstOrDefault().Name; }
                    if(Directory.Exists($"./cache/playercards/{playername.ToLower()}"))
                    {
                        //cached
                        idJObj = JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/id.json"));
                        statsJObj = JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/stats.json"));
                        ushort Id = ushort.Parse(idJObj.GetValue("id").ToString());
                        JArray Achievements = JArray.Parse(idJObj.GetValue("achievements").ToString());
                        return (statsJObj, Id, Achievements);
                    } else
                    {
                        req = await Tools.RequestApiJObject("playerById/" + playerId);
                        if (!req.Item2) { return (null, 0, null); }
                        idJObj = req.Item1;
                        if (idJObj == null) { return (null, 0, JArray.Parse("[]")); }
                    }
                }

                Directory.CreateDirectory($"./cache/playercards/{playername.ToLower()}");
                File.WriteAllText($"./cache/playercards/{playername.ToLower()}/id.json", idJObj.ToString());
                ushort playerID = ushort.Parse(idJObj.GetValue("id").ToString());
                JArray achievements = JArray.Parse(idJObj.GetValue("achievements").ToString());

                req = await Tools.RequestApiJObject("playerstats/" + playerID.ToString());
                if (!req.Item2) { return (null, 0, null); }
                statsJObj = req.Item1;
                File.WriteAllText($"./cache/playercards/{playername.ToLower()}/stats.json", statsJObj.ToString());
                return (statsJObj, playerID, achievements);
            }            
        }

        private static async Task<Embed> GetPlayerCard(SocketCommandContext context, string playername = "")
        {            
            EmbedBuilder builder = new();
            
            var req = await GetPlayerStats(playername);
            JObject jObj = req.Item1;
            JArray achievements = req.Item3;
            if (jObj == null && achievements != null) 
            {
                builder.WithColor(Color.Red)
                    .WithTitle("error")
                    .WithDescription($"The player \"{playername}\" does not exist");
                return builder.Build();
            } else if(jObj == null && achievements == null)
            {
                builder.WithColor(Color.Red)
                    .WithTitle($"error")
                    .WithDescription("Our API is currently not available! Please try again later or contact us on [github](https://github.com/Zsunamy/HLTVDiscordBridge/issues). We're sorry for the inconvience");                
                return builder.Build();
            }
            

            JObject stats = JObject.Parse(jObj.GetValue("overviewStatistics").ToString());
            JObject country = JObject.Parse(jObj.GetValue("country").ToString());
            jObj.TryGetValue("team", out JToken teamTok);
            jObj.TryGetValue("name", out JToken nameTok);
            jObj.TryGetValue("age", out JToken ageTok);
            jObj.TryGetValue("image", out JToken PBUrlTok);
            string team;
            string teamLink;
            string name;
            string age;
            if (teamTok == null) { team = "n.A"; teamLink = ""; }
            else { team = JObject.Parse(teamTok.ToString()).GetValue("name").ToString(); teamLink = $"https://www.hltv.org/team/{JObject.Parse(teamTok.ToString()).GetValue("id")}/{team.Replace(' ', '-')}"; }

            if (nameTok == null) { name = "n.A"; }
            else { name = nameTok.ToString(); }

            if (ageTok == null) { age = "n.A"; }
            else { age = ageTok.ToString(); }

            if (PBUrlTok != null) { builder.WithThumbnailUrl(PBUrlTok.ToString()); }
               

            builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://hltv.org/player/" + req.Item2.ToString() + "/" + jObj.GetValue("ign").ToString())
               .WithTitle(jObj.GetValue("ign").ToString() + $" :flag_{country.GetValue("code").ToString().ToLower()}:")
               .AddField("Name:", name, true)
               .AddField("Age:", age, true)
               .AddField("Team:", $"[{team}]({teamLink})", true)
               .AddField("Stats:", "Maps played:\nKills/Deaths:\nHeadshot %:\nADR:\nKills per round:\nAssists per round:\nDeaths per round:", true)
               .AddField("\u200b", $"{stats.GetValue("mapsPlayed")}\n{stats.GetValue("kills")}/{stats.GetValue("deaths")} ({stats.GetValue("kdRatio")})\n" +
               $"{stats.GetValue("headshots")}\n{stats.GetValue("damagePerRound")}\n {stats.GetValue("killsPerRound")}\n {stats.GetValue("assistsPerRound")}\n {stats.GetValue("deathsPerRound")}", true)
               .WithCurrentTimestamp();
            
                
            JObject ach1;
            JObject ach2;
            JObject ach3;
            string ach1Link;
            string ach2Link;
            string ach3Link;
            switch (achievements.Count)
            {
                case 0:
                    builder.AddField("Achievements:", $"none");
                    break;
                case 1:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    ach1Link = $"https://www.hltv.org/events/{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ','-').ToLower()}";
                    builder.AddField("Achievements:", $"[{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")}]({ach1Link}) finished: {ach1.GetValue("place")}");
                    break;
                case 2:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    ach2 = JObject.Parse(achievements[1].ToString());
                    ach1Link = $"https://www.hltv.org/events/{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    ach2Link = $"https://www.hltv.org/events/{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    builder.AddField("Achievements:", $"[{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")}]({ach1Link}) finished: {ach1.GetValue("place")}\n" +
                    $"[{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name")}]({ach2Link}) finished: {ach2.GetValue("place")}\n");
                    break;
                case 3:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    ach2 = JObject.Parse(achievements[1].ToString());
                    ach3 = JObject.Parse(achievements[2].ToString());
                    ach1Link = $"https://www.hltv.org/events/{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    ach2Link = $"https://www.hltv.org/events/{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    ach3Link = $"https://www.hltv.org/events/{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    builder.AddField("Achievements:", $"[{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")}]({ach1Link}) finished: {ach1.GetValue("place")}\n" +
                    $"[{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name")}]({ach2Link}) finished: {ach2.GetValue("place")}\n" +
                    $"[{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("name")}]({ach3Link}) finished: {ach3.GetValue("place")}");
                    break;
                default:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    ach2 = JObject.Parse(achievements[1].ToString());
                    ach3 = JObject.Parse(achievements[2].ToString());
                    ach1Link = $"https://www.hltv.org/events/{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    ach2Link = $"https://www.hltv.org/events/{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    ach3Link = $"https://www.hltv.org/events/{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("id")}/" +
                        $"{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("name").ToString().Replace(' ', '-').ToLower()}";
                    builder.AddField("Achievements:", $"[{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")}]({ach1Link}) finished: {ach1.GetValue("place")}\n" +
                    $"[{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name")}]({ach2Link}) finished: {ach2.GetValue("place")}\n" +
                    $"[{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("name")}]({ach3Link}) finished: {ach3.GetValue("place")}\n and {achievements.Count - 3} more");
                    break;
            }

            builder.WithFooter(Tools.GetRandomFooter(context.Guild, context.Client)); 
            bool tracked = false;
            foreach(PlayerReq plReq in StatsUpdater.StatsTracker.Players)
            {
                if(plReq.Name == jObj.GetValue("ign").ToString())
                {
                    StatsUpdater.StatsTracker.Players.Remove(plReq);
                    plReq.Reqs += 1;
                    StatsUpdater.StatsTracker.Players.Add(new PlayerReq(jObj.GetValue("ign").ToString(), int.Parse(req.Item2.ToString()), plReq.Reqs));
                    tracked = true;
                    break;
                }
            }
            if(!tracked)
            {
                StatsUpdater.StatsTracker.Players.Add(new PlayerReq(jObj.GetValue("ign").ToString(), int.Parse(req.Item2.ToString()), 1));
            }
            StatsUpdater.UpdateStats();
            return builder.Build();
        }

        [Command("player")]
        public async Task Player([Remainder]string playername = "")
        {
            EmbedBuilder builder = new();
            if (playername == "")
            {
                string prefix;
                if (Context.Channel.GetType().Equals(typeof(SocketDMChannel))) { prefix = "!"; }
                else { prefix = Config.GetServerConfig(Context.Guild).Prefix; }
                builder.WithColor(Color.Red)
                    .WithTitle("syntax error")
                    .WithDescription($"Please mind the syntax: \"{prefix}player [name]\"")
                    .WithFooter($"Example: \"{prefix}player s1mple\"");
                StatsUpdater.StatsTracker.MessagesSent += 1;
                StatsUpdater.UpdateStats();
                await ReplyAsync(embed: builder.Build());
                return;
            }

            if (!Directory.Exists($"./cache/playercards/{playername.ToLower()}"))
            {                
                builder.WithTitle("Your request is loading!")
                    .WithDescription("This may take up to 30 seconds")
                    .WithCurrentTimestamp();
                var msg = await Context.Channel.SendMessageAsync(embed: builder.Build());
                IDisposable typingState = Context.Channel.EnterTypingState();
                
                Embed embed = await GetPlayerCard(Context, playername);
                typingState.Dispose();
                await msg.DeleteAsync();
                StatsUpdater.StatsTracker.MessagesSent += 2;
                StatsUpdater.UpdateStats();
                await ReplyAsync(embed: embed);
            }
            else
            {
                StatsUpdater.StatsTracker.MessagesSent += 1;
                StatsUpdater.UpdateStats();
                await ReplyAsync(embed: await GetPlayerCard(Context, playername));
            }            
        }
    }
}
