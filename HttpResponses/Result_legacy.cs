using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.HttpResponses;

public class Result_legacy
{
    public Result_legacy(JObject jObject)
    {
        team1 = jObject.TryGetValue("team1", out JToken team1Tok)
            ? ushort.Parse(team1Tok.ToString())
            : ushort.MinValue;
        team2 = jObject.TryGetValue("team2", out JToken team2Tok)
            ? ushort.Parse(team2Tok.ToString())
            : ushort.MinValue;
    }

    public Result_legacy() {}

    public ushort team1 { get; set; }
    public ushort team2 { get; set; }
}