namespace HLTVDiscordBridge.Requests;

public class GetPlayer : ApiRequestBody<GetPlayer>
{
    protected override string Endpoint { get; } = "getPlayer";
    
    public int Id { get; set; }
}