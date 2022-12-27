namespace HLTVDiscordBridge.Requests;

public class GetPlayer : ApiRequestBody<GetPlayer>
{
    protected override string Endpoint => "GetPlayer";
    
    public int Id { get; set; }
}