namespace HLTVDiscordBridge.Requests;

public class GetPlayer : ApiRequestBody
{
    protected override string Endpoint => "GetPlayer";
    
    public int Id { get; set; }

    public GetPlayer(int id)
    {
        Id = id;
    }
}