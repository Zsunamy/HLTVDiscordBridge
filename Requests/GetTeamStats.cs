namespace HLTVDiscordBridge.Requests;

public class GetTeamStats : ApiRequestBody<GetTeamStats>
{
    protected override string Endpoint { get; } = "getTeamStats";
    public int Id { get; set; }
}