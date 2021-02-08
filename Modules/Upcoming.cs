using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Upcoming : ModuleBase<SocketCommandContext>
    {
        [Command("upcoming")]
        public async Task GetUpcoming([Remainder] string arg)
        {
            //Ausgabe nach Team oder Event oder Tag
            DateTime date;
            if (DateTime.TryParse(arg, out date))
            {
                Console.WriteLine(SearchUpcoming(date).ToString());
            }
            Console.WriteLine(SearchUpcoming(arg).ToString());
        }

        private JArray SearchUpcoming(string arg)
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            JArray result = JArray.Parse("[]");
            foreach (JToken jTok in jArr)
            {
                string eventname = JObject.Parse(JObject.Parse(jTok.ToString()).GetValue("event").ToString()).GetValue("name").ToString().ToLower();
                string team1name = JObject.Parse(JArray.Parse(JObject.Parse(jTok.ToString()).GetValue("teams").ToString())[0].ToString()).GetValue("name").ToString().ToLower();
                string team2name = JObject.Parse(JArray.Parse(JObject.Parse(jTok.ToString()).GetValue("teams").ToString())[1].ToString()).GetValue("name").ToString().ToLower();
                if (arg.ToLower() == eventname || arg.ToLower() == team1name || arg.ToLower() == team2name)
                {
                    result.Add(jTok);
                }
            }
            return result;
        }
        private JArray SearchUpcoming(DateTime date)
        {
            JArray jArr = JArray.Parse(File.ReadAllText("./cache/upcoming.json"));
            JArray result = JArray.Parse("[]");
            foreach (JToken jTok in jArr)
            {
                string time = JObject.Parse(jTok.ToString()).GetValue("time").ToString().Substring(0,10);
                if(time.Substring(0,10) == date.ToString().Substring(0, 10))
                {
                    result.Add(jTok);
                }                
            }
            return result;
        }
    }
}
