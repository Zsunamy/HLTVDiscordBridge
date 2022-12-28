namespace HLTVDiscordBridge.Requests;

public class GetPlayerByName : ApiRequestBody<GetPlayerByName>
{
    protected override string Endpoint { get; } = "getPlayerByName";
    public string Name { get; set; }
}