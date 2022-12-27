namespace HLTVDiscordBridge.Requests;

public class GetTeamByName : ApiRequestBody<GetTeamByName>
{
    protected override string Endpoint => "GetTeamByName";
    public string Name { get; set; }
}