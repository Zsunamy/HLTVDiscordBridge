namespace HLTVDiscordBridge.Requests;

public class GetTeamByName : ApiRequestBody
{
    protected override string Endpoint => "GetTeamByName";
    public string Name { get; set; }

    public GetTeamByName(string name)
    {
        Name = name;
    }
}