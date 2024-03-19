namespace HLTVDiscordBridge.Requests;

public class GetPlayerStats : ApiRequestBody<GetPlayerStats>
{
    protected override string Endpoint { get; } = "getPlayerStats";
    public int Id { get; set; }
}