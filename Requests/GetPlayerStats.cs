namespace HLTVDiscordBridge.Requests;

public class GetPlayerStats : ApiRequestBody
{
    protected override string Endpoint => "GetPlayerStats";
    public int Id { get; set; }

    public GetPlayerStats(int id)
    {
        Id = id;
    }
}