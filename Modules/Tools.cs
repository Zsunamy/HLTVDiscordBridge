using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
            Random _rnd = new();
            string footerString = footerStrings[_rnd.Next(0, footerStrings.Length)];
            builder.Text = footerString;
            return builder;
        }

        public static async Task<(JObject, bool)> RequestApiJObject(string endpoint, List<string> properties, List<string> values)
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
                return (JObject.Parse(res), true);
            }
            else
            {
                if (res.Contains("Cloudflare"))
                {
                    Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} returned cloudflare ban");
                    return (null, false);
                }
                else
                {
                    return (null, true);
                }
            }
        }
        public static async Task<(JArray, bool)> RequestApiJArray(string endpoint, List<string> properties, List<string> values)
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
                return (JArray.Parse(res), true);
            }
            else
            {
                if (res.Contains("Cloudflare"))
                {
                    Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} returned cloudflare ban");
                    return (null, false);
                }
                else
                {
                    return (null, true);
                }
            }
        }
        //overload
        public static async Task<(JArray, bool)> RequestApiJArray(string endpoint, List<string> properties, List<List<string>> values)
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
                return (JArray.Parse(res), true);
            }
            else
            {
                if (res.Contains("Cloudflare"))
                {
                    Program.WriteLog($"{DateTime.Now.ToLongTimeString()} API\t\t{endpoint} returned cloudflare ban");
                    return (null, false);
                }
                else
                {
                    return (null, true);
                }
            }
        }
    }
}
