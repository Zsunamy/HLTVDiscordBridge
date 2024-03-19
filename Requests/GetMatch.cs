namespace HLTVDiscordBridge.Requests;

public class GetMatch : ApiRequestBody<GetMatch>
{
    protected override string Endpoint { get; } = "getMatch";
    public int Id { get; set; }
}