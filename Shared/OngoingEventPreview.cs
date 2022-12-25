using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Shared
{
    public class OngoingEventPreview
    {
        public OngoingEventPreview(JObject jObject)
        {
            Id = jObject.TryGetValue("id", out JToken idTok) ? int.Parse(idTok.ToString()) : 0;
            Name = jObject.TryGetValue("name", out JToken nameTok) ? nameTok.ToString() : null;
            DateStart = jObject.TryGetValue("dateStart", out JToken dateStartTok) ? long.Parse(dateStartTok.ToString()) : 0;
            DateEnd = jObject.TryGetValue("dateEnd", out JToken dateEndTok) ? long.Parse(dateEndTok.ToString()) : 0;
            Featured = jObject.TryGetValue("featured", out JToken featuredTok) && bool.Parse(featuredTok.ToString());
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public long DateStart { get; set; }
        public long DateEnd { get; set; }
        public bool Featured { get; set; }
    }
}
