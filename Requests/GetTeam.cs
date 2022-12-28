namespace HLTVDiscordBridge.Requests;

public class GetTeam : ApiRequestBody<GetTeam>
{
    protected override string Endpoint { get; } = "getTeam";
    public int Id { get; set; }
}