using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HLTVDiscordBridge.Shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{    
    class PlayerDocument_new
    {
        public PlayerDocument_new(FullPlayer player)
        {
            PlayerId = ushort.Parse(player.id.ToString());
            Name = player.ign;
            Alias = null;
            Image = player.image;
            Nationality = player.country.code;
        }

        [BsonId]
        public ObjectId Id { get; set; }
        public ushort PlayerId { get; set; }
        public string Name { get; set; }
        public List<string> Alias { get; set; }
        public string Nationality { get; set; }
        public string Image { get; set; }
    }
    public class HltvPlayer : ModuleBase<SocketCommandContext>
    {
        private static IMongoCollection<PlayerDocument_new> GetCollection()
        {
            MongoClient dbClient = new(BotConfig.GetBotConfig().DatabaseLink);
#if DEBUG
            IMongoDatabase db = dbClient.GetDatabase("hltv-dev");
#endif
#if RELEASE
            IMongoDatabase db = dbClient.GetDatabase("hltv");
#endif
            return db.GetCollection<PlayerDocument_new>("players");
        }

        private static async Task<FullPlayer> GetPlayer_new(string playername)
        {
            List<string> properties = new(); properties.Add("name");            
            List<string> values = new(); values.Add(playername);
            try { return new FullPlayer(await Tools.RequestApiJObject("getPlayerByName", properties, values)); }
            catch (HltvApiException) { throw; }
        }
        private static async Task<(FullPlayer, FullPlayerStats)> GetPlayerStats_new(string playername)
        {            
            Directory.CreateDirectory("./cache/playercards");
            if (Directory.Exists($"./cache/playercards/{playername.ToLower()}"))
            {
                //ist lokal gespeichert
                FullPlayer player = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/id.json")));
                FullPlayerStats fullPlayerStats = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/stats.json")));
                return (player, fullPlayerStats);
            }
            else
            {
                //nicht lokal gespeichert
                FullPlayer player = new();
                FullPlayerStats fullPlayerStats = new();

                IMongoCollection<PlayerDocument_new> collection = GetCollection();
                var query1 = collection.Find(x => x.Name == playername.ToLower());
                var query2 = collection.Find(x => x.Alias.Contains(playername.ToLower()));

                List<string> properties = new(); properties.Add("name");
                List<string> values = new(); values.Add(playername);

                if (query1.CountDocuments() == 0 && query2.CountDocuments() == 0)
                {
                    //nicht in Datenbank
                    try { player = new FullPlayer(await Tools.RequestApiJObject("getPlayerByName", properties, values)); }
                    catch (HltvApiException) { throw; }

                    collection.InsertOne(new PlayerDocument_new(player));
                }
                else if (query2.CountDocuments() != 0)
                {
                    //unter Alias in Datenbank
                    playername = query2.FirstOrDefault().Name;
                    values.Clear(); values.Add(playername);
                    if (Directory.Exists($"./cache/playercards/{playername.ToLower()}"))
                    {
                        //Ist unter "richtigem Namen" doch gecached
                        player = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/id.json")));
                        fullPlayerStats = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/stats.json")));
                        return (player, fullPlayerStats);
                    }

                    try { player = new FullPlayer(await Tools.RequestApiJObject("getPlayerByName", properties, values)); }
                    catch (HltvApiException) { throw; }
                }
                else
                {
                    //in Datenbank aber nicht lokal
                    try { player = new FullPlayer(await Tools.RequestApiJObject("getPlayerByName", properties, values)); }
                    catch(HltvApiException) { throw; }
                }

                Directory.CreateDirectory($"./cache/playercards/{playername.ToLower()}");
                File.WriteAllText($"./cache/playercards/{playername.ToLower()}/id.json", JObject.FromObject(player).ToString());
                properties.Clear(); properties.Add("id");
                values.Clear(); values.Add(player.id.ToString());
                try { fullPlayerStats = new(await Tools.RequestApiJObject("getPlayerStats", properties, values)); }
                catch { throw; }
                File.WriteAllText($"./cache/playercards/{playername.ToLower()}/stats.json", JObject.FromObject(fullPlayerStats).ToString());
                return (player, fullPlayerStats);
            }
        }
        private static async Task<Embed> GetPlayerCard(string playername)
        {
            EmbedBuilder builder = new();
            (FullPlayer, FullPlayerStats) req;
            try { req = await GetPlayerStats_new(playername); }
            catch (Exception) { throw; }

            FullPlayer fullPlayer = req.Item1;
            FullPlayerStats fullPlayerStats = req.Item2;

            if (fullPlayerStats.image != null) { builder.WithThumbnailUrl(fullPlayerStats.image); }
            if (fullPlayerStats.id != 0 && fullPlayerStats.ign != null) 
            { builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", 
                $"https://hltv.org/player/{fullPlayerStats.id}/{fullPlayerStats.ign}"); 
            }
            if(fullPlayerStats.country.code != null && fullPlayerStats.ign != null) { builder.WithTitle(fullPlayerStats.ign + $" :flag_{fullPlayerStats.country.code}:"); }
            if(fullPlayerStats.name != null) { builder.AddField("Name:", fullPlayerStats.name, true); }
            if(fullPlayerStats.age != null) { builder.AddField("Age:", fullPlayerStats.age, true); } else { builder.AddField("\u200b", "\u200b"); }
            if(fullPlayerStats.team != null) { builder.AddField("Team:", $"[{fullPlayerStats.team.name}]({fullPlayerStats.team.link})", true); } else { builder.AddField("Team:", "none"); }
            if (fullPlayerStats.overviewStatistics != null) 
            {
                builder.AddField("Stats:", "Maps played:\nKills/Deaths:\nHeadshot %:\nADR:\nKills per round:\nAssists per round:\nDeaths per round:", true);
                builder.AddField("\u200b", $"{fullPlayerStats.overviewStatistics.mapsPlayed}\n{fullPlayerStats.overviewStatistics.kills}/{fullPlayerStats.overviewStatistics.deaths} " +
                    $"({fullPlayerStats.overviewStatistics.kdRatio})\n{fullPlayerStats.overviewStatistics.headshots}\n{fullPlayerStats.overviewStatistics.damagePerRound}\n " +
                    $"{fullPlayerStats.overviewStatistics.killsPerRound}\n {fullPlayerStats.overviewStatistics.assistsPerRound}\n {fullPlayerStats.overviewStatistics.deathsPerRound}", true);
            }
            builder.WithCurrentTimestamp();

            if(fullPlayer.achievements.Count != 0)
            {
                List<string> achievements = new();
                foreach(Achievement achievement in fullPlayer.achievements)
                {             
                    if(string.Join("\n", achievements).Length > 600) { break; }
                    achievements.Add($"[{achievement.eventObj.name}]({achievement.eventObj.link}) finished: {achievement.place}");
                }
                builder.AddField("Achievements:", string.Join("\n", achievements));

            } else
            {
                builder.AddField("Achievements:", $"none");
            }
            builder.WithFooter(Tools.GetRandomFooter());

            bool tracked = false;
            foreach (PlayerReq plReq in StatsUpdater.StatsTracker.Players)
            {
                if (plReq.Name == fullPlayerStats.ign)
                {
                    StatsUpdater.StatsTracker.Players.Remove(plReq);
                    plReq.Reqs += 1;
                    StatsUpdater.StatsTracker.Players.Add(new PlayerReq(fullPlayerStats.ign, (int)fullPlayerStats.id, plReq.Reqs));
                    tracked = true;
                    break;
                }
            }
            if (!tracked)
            {
                StatsUpdater.StatsTracker.Players.Add(new PlayerReq(fullPlayerStats.ign, (int)fullPlayerStats.id, 1));
            }
            StatsUpdater.UpdateStats();
            return builder.Build();
        }        
        public static async Task SendPlayerCard(SocketSlashCommand arg)
        {

            if (!Directory.Exists($"./cache/playercards/{arg.Data.Options.First().Value.ToString()?.ToLower()}"))
            {
                await arg.DeferAsync();

                Embed embed;
                try { embed = await GetPlayerCard(arg.Data.Options.First().Value.ToString()); }
                catch(HltvApiException e) { embed = ErrorHandling.GetErrorEmbed(e); }
                

                StatsUpdater.StatsTracker.MessagesSent += 2;
                StatsUpdater.UpdateStats();
                await arg.ModifyOriginalResponseAsync(msg => msg.Embed = embed);
            }
            else
            {
                StatsUpdater.StatsTracker.MessagesSent += 1;
                StatsUpdater.UpdateStats();
                await arg.RespondAsync(embed: await GetPlayerCard(arg.Data.Options.First().Value.ToString()));
            }
        }
    }
}
