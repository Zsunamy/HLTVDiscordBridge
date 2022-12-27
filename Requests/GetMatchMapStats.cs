namespace HLTVDiscordBridge.Requests;

public class GetMatchMapStats : ApiRequestBody<GetMatchMapStats>
{
    protected override string Endpoint => "GetMatchMapStats";
    public int Id { get; set; }
}