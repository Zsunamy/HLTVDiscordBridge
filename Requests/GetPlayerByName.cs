namespace HLTVDiscordBridge.Requests;

public class GetPlayerByName : ApiRequestBody<GetPlayerByName>
{
    protected override string Endpoint => "GetPlayerByName";
    public string Name { get; set; }
}