namespace HLTVDiscordBridge.Requests;

public class GetMatches : ApiRequestBody<GetMatches>
{
    protected override string Endpoint { get; } = "getMatches";
    public int EventId { get; set; }
    public string EventType { get; set; }
    public string MatchFilter { get; set; }
    public int[] TeamIds { get; set; }
}