namespace HLTVDiscordBridge.Requests;

public class GetMatchMapStats : ApiRequestBody<GetMatchMapStats>
{
    protected override string Endpoint { get; } = "getMatchMapStats";
    public int Id { get; set; }
}