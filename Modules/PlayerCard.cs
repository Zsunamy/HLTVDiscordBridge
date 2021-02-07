using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Modules
{
    public class PlayerCard : ModuleBase<SocketCommandContext>
    {
        private async Task<JObject> GetPlayerStats(string playername)
        {
            //gets PlayerID
            ushort playerID = await GetPlayerID(playername);   
            if(playerID == 0) { return null; }
            Uri uri = new Uri("https://hltv-api-steel.vercel.app/api/playerstats/" + playerID.ToString());
            HttpClient _http = new HttpClient();
            _http.BaseAddress = uri;
            HttpResponseMessage httpRequest = await _http.GetAsync(uri);
            JObject jObj = JObject.Parse(await httpRequest.Content.ReadAsStringAsync());
            return jObj;
        }
        private async Task<JArray> GetAchievements(string playername)
        {
            Uri uri = new Uri("https://hltv-api-steel.vercel.app/api/player/" + playername);
            HttpClient _http = new HttpClient();
            _http.BaseAddress = uri;
            HttpResponseMessage httpRequest = await _http.GetAsync(uri);
            return JArray.Parse(JObject.Parse(await httpRequest.Content.ReadAsStringAsync()).GetValue("achievements").ToString());
        }

        private async Task<ushort> GetPlayerID(string playername)
        {
            Uri uri = new Uri("https://hltv-api-steel.vercel.app/api/player/" + playername);
            HttpClient _http = new HttpClient();
            _http.BaseAddress = uri;
            HttpResponseMessage httpRequest = await _http.GetAsync(uri);
            JObject jObj = JObject.Parse(await httpRequest.Content.ReadAsStringAsync());
            if (jObj.Count == 0) { return 0; }
            return ushort.Parse(jObj.GetValue("id").ToString());
        }

        private async Task<Embed> GetPlayerCard(string playername = "")
        {
            EmbedBuilder builder = new EmbedBuilder();
            if (playername == "")
            {
                builder.WithColor(Color.Red)
                    .WithTitle("SYNTAX ERROR")
                    .WithDescription("Please mind the syntax: \"!player [name]\"");
                return builder.Build();
            }            
            JObject jObj = await GetPlayerStats(playername);
            if (jObj == null) 
            {
                builder.WithColor(Color.Red)
                    .WithTitle("ERROR")
                    .WithDescription($"The player \"{playername}\" does not exist");
                return builder.Build();
            }
            JArray achievements = await GetAchievements(jObj.GetValue("ign").ToString());
            
            JObject stats = JObject.Parse(jObj.GetValue("statistics").ToString());
            JObject team = JObject.Parse(jObj.GetValue("team").ToString());
            JObject country = JObject.Parse(jObj.GetValue("country").ToString());
            jObj.TryGetValue("name", out JToken nameTok);
            jObj.TryGetValue("age", out JToken ageTok);
            jObj.TryGetValue("image", out JToken PBUrlTok);
            string name;
            string age;
            if (nameTok == null)
                name = "n.A";
            else
                name = nameTok.ToString();
            if (ageTok == null)
                age = "n.A";
            else
                age = ageTok.ToString();
            if (PBUrlTok != null)
                builder.WithThumbnailUrl(PBUrlTok.ToString());

            builder.WithAuthor("more info on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://hltv.org/player/" + (await GetPlayerID(jObj.GetValue("ign").ToString())).ToString() + "/" + jObj.GetValue("ign").ToString())
               .WithTitle(jObj.GetValue("ign").ToString() + $" :flag_{country.GetValue("code").ToString().ToLower()}:")
               .AddField("Name:", name, true)
               .AddField("Age:", age, true)
               .AddField("Team:", team.GetValue("name").ToString(), true)
               .AddField("Stats:", "Maps played:\nKills/Deaths:\nHeadshot %:\nADR:\nKills per round:\nAssists per round:\nDeaths per round:", true)
               .AddField("\u200b", $"{stats.GetValue("mapsPlayed")}\n{stats.GetValue("kills")}/{stats.GetValue("deaths")} ({stats.GetValue("kdRatio")})\n" +
               $"{stats.GetValue("headshots")}\n{stats.GetValue("damagePerRound")}\n {stats.GetValue("killsPerRound")}\n {stats.GetValue("assistsPerRound")}\n {stats.GetValue("deathsPerRound")}", true)
               .WithCurrentTimestamp();
            
                
            JObject ach1;
            JObject ach2;
            JObject ach3;
            switch (achievements.Count)
            {
                case 0:
                    builder.AddField("Achievements:", $"none");
                    break;
                case 1:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    builder.AddField("Achievements:", $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")} finished: {ach1.GetValue("place")}");
                    break;
                case 2:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    ach2 = JObject.Parse(achievements[1].ToString());
                    builder.AddField("Achievements:", $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")} finished: {ach1.GetValue("place")}\n" +
                    $"{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name")} finished: {ach2.GetValue("place")}\n");
                    break;
                case 3:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    ach2 = JObject.Parse(achievements[1].ToString());
                    ach3 = JObject.Parse(achievements[2].ToString());
                    builder.AddField("Achievements:", $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")} finished: {ach1.GetValue("place")}\n" +
                    $"{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name")} finished: {ach2.GetValue("place")}\n" +
                    $"{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("name")} finished: {ach3.GetValue("place")}");
                    break;
                default:
                    ach1 = JObject.Parse(achievements[0].ToString());
                    ach2 = JObject.Parse(achievements[1].ToString());
                    ach3 = JObject.Parse(achievements[2].ToString());
                    builder.AddField("Achievements:", $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")} finished: {ach1.GetValue("place")}\n" +
                    $"{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name")} finished: {ach2.GetValue("place")}\n" +
                    $"{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("name")} finished: {ach3.GetValue("place")}\n and {achievements.Count - 3} more");
                    break;
            }
            

            return builder.Build();
        }

        [Command("player")]
        public async Task Player(string playername)
        {
            await ReplyAsync("", false, await GetPlayerCard(playername));
        }
    }
}
