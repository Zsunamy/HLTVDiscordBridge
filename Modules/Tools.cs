using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Modules
{
    public class Tools
    {
        public static EmbedFooterBuilder GetRandomFooter ()
        {
            EmbedFooterBuilder builder = new();
            string[] footerStrings = File.ReadAllText("./cache/footer.txt").Split("\n");
            Random random = new();
            string footerString = footerStrings[random.Next(0, footerStrings.Length)];
            builder.Text = footerString;
            return builder;
        }
        public static async Task<JObject> RequestApiJObject(string endpoint, List<string> properties, List<string> values)
        {
            HttpClient http = new();

            Uri uri = new($"{Config.LoadConfig().APILink}/api/{endpoint}");

            StringBuilder sb = new();
            StringWriter sw = new(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("delayBetweenPageRequests");
                writer.WriteValue(300);
                for(int i = 0; i < properties.Count; i++)
                {
                    writer.WritePropertyName(properties[i]);
                    writer.WriteValue(values[i]);
                }
                writer.WriteEndObject();
            }
            
            HttpResponseMessage resp = await http.PostAsync(uri, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
            string res = await resp.Content.ReadAsStringAsync();
            if(resp.IsSuccessStatusCode)
            {
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JObject.Parse(res);
            }
            else
            {
                try
                {
                    var error = JObject.Parse(await resp.Content.ReadAsStringAsync());
                    throw new HltvApiException(error);
                }
                catch(JsonReaderException) { throw new Exception("Deployment Error"); }                
            }
        }
        public static async Task<JArray> RequestApiJArray(string endpoint, List<string> properties, List<string> values)
        {
            HttpClient http = new();
            Uri uri = new($"{Config.LoadConfig().APILink}/api/{endpoint}");

            StringBuilder sb = new();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("delayBetweenPageRequests");
                writer.WriteValue(300);
                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        writer.WritePropertyName(properties[i]);
                        writer.WriteValue(values[i]);
                    }
                }
                writer.WriteEndObject();
            }

            HttpResponseMessage resp = await http.PostAsync(uri, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
            string res = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JArray.Parse(res);
            }
            else
            {
                try
                {
                    var error = JObject.Parse(await resp.Content.ReadAsStringAsync());
                    throw new HltvApiException(error);
                }
                catch (JsonReaderException) { throw new Exception("Deployment Error"); }
            }
        }
        public static async Task<JArray> RequestApiJArray(string endpoint, List<string> properties, List<List<string>> values)
        {
            HttpClient http = new();
            Uri uri = new($"{Config.LoadConfig().APILink}/api/{endpoint}");

            StringBuilder sb = new();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("delayBetweenPageRequests");
                writer.WriteValue(300);
                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        writer.WritePropertyName(properties[i]);
                        writer.WriteStartArray();
                        foreach(string s in values[i])
                            writer.WriteValue(s);
                        writer.WriteEndArray();
                    }
                }
                writer.WriteEndObject();
            }

            HttpResponseMessage resp = await http.PostAsync(uri, new StringContent(sb.ToString(), Encoding.UTF8, "application/json"));
            string res = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} was successful");
                StatsUpdater.StatsTracker.ApiRequest = +1;
                StatsUpdater.UpdateStats();
                return JArray.Parse(res);
            }
            else
            {
                try
                {
                    var error = JObject.Parse(await resp.Content.ReadAsStringAsync());
                    throw new HltvApiException(error);
                }
                catch (JsonReaderException) { throw new Exception("Deployment Error"); }
            }
        }

        public static string GetHltvTimeFormat(DateTime date)
        {
            string day = date.Day.ToString();
            string month = date.Month.ToString();
            if (day.Length == 1)
            {
                day = $"0{day}";
            }
            
            if (month.Length == 1)
            {
                month = $"0{month}";
            }

            return $"{date.Year.ToString()}-{month}-{day}";
        }

        public static int GetIdFromUrl(string url)
        {
            return int.Parse(url.Split('/')[^2]);
        }
    }
}
