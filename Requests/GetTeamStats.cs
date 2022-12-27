namespace HLTVDiscordBridge.Requests;

public class GetTeamStats : ApiRequestBody<GetTeamStats>
{
    protected override string Endpoint => "GetTeamStats";
    public int Id { get; set; }
}