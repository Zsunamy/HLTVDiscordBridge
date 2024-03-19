namespace HLTVDiscordBridge.Requests;

public class GetEvent : ApiRequestBody<GetEvent>
{
    protected override string Endpoint { get; }= "getEvent";
    public int Id { get; set; }
}