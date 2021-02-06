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
            Uri uri = new Uri("https://hltv-api.revilum.com/playerstats/" + playerID.ToString());
            HttpClient _http = new HttpClient();
            _http.BaseAddress = uri;
            HttpResponseMessage httpRequest = await _http.GetAsync(uri);
            JObject jObj = JObject.Parse(await httpRequest.Content.ReadAsStringAsync());
            return jObj;
        }
        private async Task<JArray> GetAchievements(string playername)
        {
            Uri uri = new Uri("https://hltv-api.revilum.com/player/" + playername);
            HttpClient _http = new HttpClient();
            _http.BaseAddress = uri;
            HttpResponseMessage httpRequest = await _http.GetAsync(uri);
            return JArray.Parse(JObject.Parse(await httpRequest.Content.ReadAsStringAsync()).GetValue("achievements").ToString());
        }

        private async Task<ushort> GetPlayerID(string playername)
        {
            Uri uri = new Uri("https://hltv-api.revilum.com/player/" + playername);
            HttpClient _http = new HttpClient();
            _http.BaseAddress = uri;
            HttpResponseMessage httpRequest = await _http.GetAsync(uri);
            return ushort.Parse(JObject.Parse(await httpRequest.Content.ReadAsStringAsync()).GetValue("id").ToString());
        }

        private async Task<Embed> GetPlayerCard(string playername)
        {
            EmbedBuilder builder = new EmbedBuilder();
            JObject jObj = await GetPlayerStats(playername);
            JArray achievements = await GetAchievements(jObj.GetValue("ign").ToString());
            JObject ach1 = JObject.Parse(achievements[0].ToString());
            JObject ach2 = JObject.Parse(achievements[1].ToString());
            JObject ach3 = JObject.Parse(achievements[2].ToString());
            JObject stats = JObject.Parse(jObj.GetValue("statistics").ToString());
            JObject team = JObject.Parse(jObj.GetValue("team").ToString());
            JObject country = JObject.Parse(jObj.GetValue("country").ToString());            
            builder.WithAuthor("more infos on hltv.org", "https://www.hltv.org/img/static/TopLogoDark2x.png", "https://hltv.org/player/" + (await GetPlayerID(jObj.GetValue("ign").ToString())).ToString() + "/" + jObj.GetValue("ign").ToString())
                .WithTitle(jObj.GetValue("ign").ToString() + $" :flag_{country.GetValue("code").ToString().ToLower()}:")
                .WithThumbnailUrl(jObj.GetValue("image").ToString())
                .AddField("IRL name:", jObj.GetValue("name").ToString(), true)
                .AddField("Age:", jObj.GetValue("age").ToString(), true)
                .AddField("Team:", team.GetValue("name").ToString(), true)
                .AddField("Stats:", "Maps played:\nKills/Deaths:\nHeadshot %:\nADR:\nKills per round:\nAssists per round:\nDeaths per round:", true)
                .AddField("amk", $"{stats.GetValue("mapsPlayed")}\n{stats.GetValue("kills")}/{stats.GetValue("deaths")} ({stats.GetValue("kdRatio")})\n" +
                $"{stats.GetValue("headshots")}\n{stats.GetValue("damagePerRound")}\n {stats.GetValue("killsPerRound")}\n {stats.GetValue("assistsPerRound")}\n {stats.GetValue("deathsPerRound")}", true)
                .AddField("Achievements:", $"{JObject.Parse(ach1.GetValue("event").ToString()).GetValue("name")} finished: {ach1.GetValue("place")}\n" +
                $"{JObject.Parse(ach2.GetValue("event").ToString()).GetValue("name")} finished: {ach2.GetValue("place")}\n" +
                $"{JObject.Parse(ach3.GetValue("event").ToString()).GetValue("name")} finished: {ach3.GetValue("place")}\n and {achievements.Count - 3} more");

            return builder.Build();
        }

        [Command("player")]
        public async Task Player(string playername)
        {
            await ReplyAsync("", false, await GetPlayerCard(playername));
        }
    }
}
