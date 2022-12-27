namespace HLTVDiscordBridge.Requests;

public class GetEvent : ApiRequestBody<GetEvent>
{
    protected override string Endpoint => "getEvent";
    public int Id { get; set; }
}