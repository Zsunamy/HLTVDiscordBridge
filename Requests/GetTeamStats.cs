namespace HLTVDiscordBridge.Requests;

public class GetTeamStats : ApiRequestBody
{
    public int Id;
    protected override string Endpoint => "GetTeamStats";

    public GetTeamStats(int id)
    {
        Id = id;
    }
}