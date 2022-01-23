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
using System.Text;
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
            MongoClient dbClient = new(Config.LoadConfig().DatabaseLink);
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
            return new FullPlayer((await Tools.RequestApiJObject("getPlayerByName", properties, values)).Item1);
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
                    try { player = new FullPlayer((await Tools.RequestApiJObject("getPlayerByName", properties, values)).Item1); } 
                    catch (Exception) { throw; }

                    collection.InsertOne(new PlayerDocument_new(player));
                }
                else if(query2.CountDocuments() != 0)
                {
                    //unter Alias in Datenbank
                    playername = query2.FirstOrDefault().Name;
                    if (Directory.Exists($"./cache/playercards/{playername.ToLower()}"))
                    {
                        //Ist unter "richtigem Namen" doch gecached
                        player = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/id.json")));
                        fullPlayerStats = new(JObject.Parse(File.ReadAllText($"./cache/playercards/{playername.ToLower()}/stats.json")));
                        return (player, fullPlayerStats);
                    }

                    player = new FullPlayer((await Tools.RequestApiJObject("getPlayerByName", properties, values)).Item1);
                }
                else
                {
                    //in Datenbank aber nicht lokal
                    player = new FullPlayer((await Tools.RequestApiJObject("getPlayerByName", properties, values)).Item1);
                }

                Directory.CreateDirectory($"./cache/playercards/{playername.ToLower()}");
                File.WriteAllText($"./cache/playercards/{playername.ToLower()}/id.json", JObject.FromObject(player).ToString());
                properties.Clear(); properties.Add("id");
                values.Clear(); values.Add(player.id.ToString());

                fullPlayerStats = new((await Tools.RequestApiJObject("getPlayerStats", properties, values)).Item1);
                File.WriteAllText($"./cache/playercards/{playername.ToLower()}/stats.json", JObject.FromObject(fullPlayerStats).ToString());
                return (player, fullPlayerStats);
            }
        }
        private static async Task<Embed> GetPlayerCard(string playername)
        {
            EmbedBuilder builder = new();
            (FullPlayer, FullPlayerStats) req;
            try { req = await GetPlayerStats_new(playername); }
            catch (Exception ex) { return null; }

            FullPlayer fullPlayer = req.Item1;
            FullPlayerStats fullPlayerStats = req.Item2;

            if (fullPlayerStats.image != null) { builder.WithThumbnailUrl(fullPlayerStats.image); }
            if (fullPlayerStats.id != 0 && fullPlayerStats.ign != null) 
            { builder.WithAuthor("click here for more details", "https://www.hltv.org/img/static/TopLogoDark2x.png", 
                $"https://hltv.org/player/{fullPlayerStats.id}/{fullPlayerStats.ign}"); 
            }
            if(fullPlayerStats.country.code != null && fullPlayerStats.ign != null) { builder.WithTitle(fullPlayerStats.ign + $" :flag_{fullPlayerStats.country.code}:"); }
            if(fullPlayerStats.name != null) { builder.AddField("Name:", fullPlayerStats.name, true); }
            if(fullPlayerStats.age != null) { builder.AddField("Age:", fullPlayerStats.age, true); }
            if(fullPlayerStats.team != null) { builder.AddField("Team:", $"[{fullPlayerStats.team.name}]({fullPlayerStats.team.link})", true); }
            if(fullPlayerStats.overviewStatistics != null) 
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
        
        /*[Command("player")]
        public async Task Player([Remainder] string playername = "")
        {
            EmbedBuilder builder = new();

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
        }*/

        public static async Task SendPlayerCard(SocketSlashCommand arg)
        {
            EmbedBuilder builder = new();

            if (!Directory.Exists($"./cache/playercards/{arg.Data.Options.First().Value.ToString().ToLower()}"))
            {
                builder.WithTitle("Your request is loading!")
                    .WithDescription("This may take up to 30 seconds")
                    .WithCurrentTimestamp();
                //var msg = await arg.Channel.SendMessageAsync(embed: builder.Build());
                await arg.DeferAsync();
                //IDisposable typingState = arg.Channel.EnterTypingState();

                Embed embed = await GetPlayerCard(arg.Data.Options.First().Value.ToString());
                //typingState.Dispose();
                //await msg.DeleteAsync();
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
