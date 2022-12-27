namespace HLTVDiscordBridge.Requests;

public class GetPlayerStats : ApiRequestBody<GetPlayerStats>
{
    protected override string Endpoint => "GetPlayerStats";
    public int Id { get; set; }
}