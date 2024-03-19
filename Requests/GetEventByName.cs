namespace HLTVDiscordBridge.Requests;

public class GetEventByName : ApiRequestBody<GetEventByName>
{
    protected override string Endpoint { get; } = "getEventByName";
    public string Name { get; set; }
}