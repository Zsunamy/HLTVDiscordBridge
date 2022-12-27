namespace HLTVDiscordBridge.Requests;

public class GetEventByName : ApiRequestBody<GetEventByName>
{
    protected override string Endpoint => "GetEventByName";
    public string Name { get; set; }
}