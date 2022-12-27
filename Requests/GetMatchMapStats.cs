namespace HLTVDiscordBridge.Requests;

public class GetMatchMapStats : ApiRequestBody
{
    protected override string Endpoint => "GetMatchMapStats";
    
    public int Id { get; set; }

    public GetMatchMapStats(int id)
    {
        Id = id;
    }
}