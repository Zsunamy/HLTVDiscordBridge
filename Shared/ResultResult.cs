using Newtonsoft.Json.Linq;

namespace HLTVDiscordBridge.Shared;

public class ResultResult
{
    public int Team1;
    public int Team2;
    public ResultResult(JObject jObject)
    {
        Team1 = jObject.TryGetValue("team1", out JToken team1Tok)
            ? ushort.Parse(team1Tok.ToString())
            : ushort.MinValue;
        Team2 = jObject.TryGetValue("team2", out JToken team2Tok)
            ? ushort.Parse(team2Tok.ToString())
            : ushort.MinValue;
    }
}