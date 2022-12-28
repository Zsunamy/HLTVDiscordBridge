namespace HLTVDiscordBridge.Requests;

public class GetMatchStats : ApiRequestBody<GetMatchStats>
{
    protected override string Endpoint { get; } = "getMatchStats";
    public int Id { get; set; }
}