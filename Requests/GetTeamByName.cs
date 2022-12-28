namespace HLTVDiscordBridge.Requests;

public class GetTeamByName : ApiRequestBody<GetTeamByName>
{
    protected override string Endpoint { get; } = "getTeamByName";
    public string Name { get; set; }
}